using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.CirculationPolicies;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Reservations;

public sealed class ReservationService(
    IReservationRepository reservations,
    IBorrowingRepository borrowings,
    IBookRepository books,
    IMemberRepository members,
    ICirculationPolicyResolver policyResolver,
    TimeProvider timeProvider)
{
    public async Task<ReservationPageModel> GetAsync(ReservationListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (items, totalCount) = await reservations.GetPageAsync(
            query.Search,
            query.Status,
            pageNumber,
            pageSize,
            now,
            cancellationToken);

        var models = new List<ReservationModel>(items.Count);
        foreach (var item in items)
            models.Add(await MapAsync(item, cancellationToken));

        return new ReservationPageModel(models, pageNumber, pageSize, totalCount);
    }

    public async Task<ReservationDetailModel?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null) return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        var member = await members.GetByIdAsync(reservation.ReserverId, cancellationToken);
        var activeReservations = await reservations.GetActiveReservationsForBookAsync(reservation.BookId, now, cancellationToken);
        var queuePos = await reservations.GetQueuePositionAsync(reservation.BookId, reservation.Id, now, cancellationToken);

        var resModel = ToModel(
            reservation,
            book?.Title ?? "Unknown book",
            book?.Quantity ?? 0,
            now,
            queuePos,
            book?.Author,
            book?.Category,
            member?.MemberCode,
            member?.MembershipCard?.CardNumber,
            reservation.ConcurrencyToken);

        var bookQueueModels = new List<ReservationModel>(activeReservations.Count);
        for (var i = 0; i < activeReservations.Count; i++)
        {
            var item = activeReservations[i];
            bookQueueModels.Add(ToModel(
                item,
                book?.Title ?? "Unknown book",
                book?.Quantity ?? 0,
                now,
                i + 1,
                book?.Author,
                book?.Category,
                null,
                null,
                item.ConcurrencyToken));
        }

        var holdDays = (int)Math.Round((reservation.ExpiresAtUtc - reservation.ReservedAtUtc).TotalDays);
        if (holdDays < 1) holdDays = 7;

        return new ReservationDetailModel(
            resModel,
            book?.Isbn,
            member?.MemberGroup,
            book?.Quantity ?? 0,
            activeReservations.Count,
            "Chính sách lưu thông tiêu chuẩn",
            holdDays,
            bookQueueModels);
    }

    public async Task<ReservationResult> CreateAsync(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Sách không tồn tại trong hệ thống.");

        var reserver = await members.GetByIdAsync(command.ReserverId, cancellationToken);
        if (reserver is null || reserver.Status != MemberStatus.Active)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Độc giả không tồn tại hoặc đã bị vô hiệu hóa.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (reserver.MembershipCard is null || reserver.MembershipCard.Status != MembershipCardStatus.Active || reserver.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Thẻ độc giả chưa được kích hoạt hoặc đã hết hạn.");

        if (reserver.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) && x.Type is MemberRestrictionType.Reservation or MemberRestrictionType.AllTransactions))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả đang có giới hạn hoặc bị khóa quyền đặt trước sách.");

        if (await reservations.HasOpenReservationAsync(command.BookId, command.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả này đã có một yêu cầu đặt trước đang chờ đối với cuốn sách này.");

        if (await borrowings.HasActiveBorrowingAsync(command.BookId, command.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả này hiện đang mượn cuốn sách này, không thể đặt trước thêm.");

        try
        {
            var policy = await policyResolver.ResolveAsync(reserver.MemberGroup, book.Category, null, now, cancellationToken);
            var holdDays = command.HoldDays <= 0 ? policy.HoldDays : Math.Min(command.HoldDays, policy.HoldDays);
            var reservation = Reservation.Create(
                book.Id,
                reserver.Id,
                reserver.FullName,
                reserver.Email,
                now,
                holdDays,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));

            await reservations.AddAsync(reservation, cancellationToken);

            var auditLog = AuditLog.Create(
                command.ActorUserId ?? command.ReserverId,
                "reservation.created",
                "Reservation",
                reservation.Id,
                null,
                JsonSerializer.Serialize(new
                {
                    reservation.Id,
                    reservation.BookId,
                    reservation.ReserverId,
                    reservation.ReservedAtUtc,
                    reservation.ExpiresAtUtc,
                    HoldDays = holdDays
                }),
                now);
            await reservations.AddAuditLogAsync(auditLog, cancellationToken);

            await reservations.SaveChangesAsync(cancellationToken);

            var queuePos = await reservations.GetQueuePositionAsync(book.Id, reservation.Id, now, cancellationToken);
            return ReservationResult.Success(ToModel(
                reservation,
                book.Title,
                book.Quantity,
                now,
                queuePos,
                book.Author,
                book.Category,
                reserver.MemberCode,
                reserver.MembershipCard?.CardNumber,
                reservation.ConcurrencyToken));
        }
        catch (ArgumentException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Validation, exception.Message);
        }
    }

    public Task<ReservationResult> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        CancelAsync(id, new CancelReservationCommand(Guid.Empty, null, Guid.Empty), cancellationToken);

    public async Task<ReservationResult> CancelAsync(Guid id, CancelReservationCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Không tìm thấy thông tin đặt trước.");

        if (command.ConcurrencyToken != Guid.Empty && reservation.ConcurrencyToken != command.ConcurrencyToken)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Dữ liệu đặt trước đã bị thay đổi bởi thao tác khác. Vui lòng tải lại trang.");

        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        var member = await members.GetByIdAsync(reservation.ReserverId, cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var beforeJson = JsonSerializer.Serialize(new
            {
                reservation.Id,
                Status = reservation.IsFulfilled ? "fulfilled" : reservation.IsCancelled ? "cancelled" : reservation.IsExpired(now) ? "expired" : "active",
                reservation.FulfilledAtUtc,
                reservation.CancelledAtUtc,
                reservation.ConcurrencyToken
            });

            reservation.MarkCancelled(now);

            var afterJson = JsonSerializer.Serialize(new
            {
                reservation.Id,
                Status = "cancelled",
                reservation.CancelledAtUtc,
                Reason = command.Reason,
                reservation.ConcurrencyToken
            });

            var actorId = command.ActorUserId == Guid.Empty ? (Guid?)null : command.ActorUserId;
            var auditLog = AuditLog.Create(
                actorId,
                "reservation.cancelled",
                "Reservation",
                reservation.Id,
                beforeJson,
                afterJson,
                now);
            await reservations.AddAuditLogAsync(auditLog, cancellationToken);

            await reservations.SaveChangesAsync(cancellationToken);
            return ReservationResult.Success(ToModel(
                reservation,
                book?.Title ?? "Unknown book",
                book?.Quantity ?? 0,
                now,
                0,
                book?.Author,
                book?.Category,
                member?.MemberCode,
                member?.MembershipCard?.CardNumber,
                reservation.ConcurrencyToken));
        }
        catch (InvalidOperationException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Conflict, exception.Message);
        }
    }

    public Task<ReservationResult> FulfillAsync(Guid id, CancellationToken cancellationToken) =>
        FulfillAsync(id, new FulfillReservationCommand(Guid.Empty, null, Guid.Empty), cancellationToken);

    public async Task<ReservationResult> FulfillAsync(Guid id, FulfillReservationCommand command, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Không tìm thấy thông tin đặt trước.");

        if (command.ConcurrencyToken != Guid.Empty && reservation.ConcurrencyToken != command.ConcurrencyToken)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Dữ liệu đặt trước đã bị thay đổi bởi thao tác khác. Vui lòng tải lại trang.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (reservation.IsExpired(now))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Phiếu đặt trước đã quá hạn giữ sách và đã hết hiệu lực.");

        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        if (book is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Không tìm thấy đầu sách tương ứng.");

        var member = await members.GetByIdAsync(reservation.ReserverId, cancellationToken);
        if (member is null || member.Status != MemberStatus.Active || member.MembershipCard?.Status != MembershipCardStatus.Active)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Thành viên hoặc thẻ độc giả không ở trạng thái hoạt động.");

        var history = await members.GetHistoryAsync(member.Id, cancellationToken);
        var policy = await policyResolver.ResolveAsync(member.MemberGroup, book.Category, null, now, cancellationToken);
        if (history.Borrowings.Count(x => !x.IsReturned) >= Math.Min(member.BorrowingLimit, policy.MaxLoanBooks))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả đã đạt số lượng sách mượn tối đa cho phép.");

        if (policy.BlockIfOverdue && history.Borrowings.Any(x => x.IsOverdue(now)))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả đang có sách quá hạn chưa trả, chính sách lưu thông từ chối cho nhận sách.");

        if (await borrowings.HasActiveBorrowingAsync(reservation.BookId, reservation.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Độc giả này hiện đang mượn một bản của cuốn sách này.");

        BookCopy? copy = null;
        if (!string.IsNullOrWhiteSpace(command.BookCopyBarcode))
        {
            copy = await reservations.GetAvailableBookCopyByBarcodeAsync(book.Id, command.BookCopyBarcode.Trim(), cancellationToken);
            if (copy is null)
                return ReservationResult.Fail(ReservationFailure.Conflict, $"Bản sao có mã vạch '{command.BookCopyBarcode}' không khả dụng hoặc không thuộc đầu sách này.");
        }
        else
        {
            copy = await reservations.GetFirstAvailableBookCopyAsync(book.Id, cancellationToken);
        }

        if (copy is null && book.Quantity <= 0)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Không còn bản sao khả dụng nào trong kho để hoàn tất nhận sách.");

        try
        {
            var beforeJson = JsonSerializer.Serialize(new
            {
                reservation.Id,
                reservation.FulfilledAtUtc,
                reservation.ConcurrencyToken,
                BookQuantity = book.Quantity
            });

            if (copy is not null)
            {
                copy.Checkout(now);
                book.Checkout(now);
            }
            else
            {
                book.Checkout(now);
            }

            reservation.MarkFulfilled(now);

            var loanDays = Math.Min(member.LoanPeriodDays, policy.LoanPeriodDays);
            var actorId = command.ActorUserId == Guid.Empty ? (Guid?)null : command.ActorUserId;

            var borrowing = Borrowing.CreateWithCopy(
                book.Id,
                copy?.Id,
                reservation.ReserverId,
                reservation.ReserverName,
                reservation.ReserverEmail,
                now,
                loanDays,
                actorId,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));

            await borrowings.AddAsync(borrowing, cancellationToken);

            var resAudit = AuditLog.Create(
                actorId,
                "reservation.fulfilled",
                "Reservation",
                reservation.Id,
                beforeJson,
                JsonSerializer.Serialize(new
                {
                    reservation.Id,
                    Status = "fulfilled",
                    reservation.FulfilledAtUtc,
                    BorrowingId = borrowing.Id,
                    CopyBarcode = copy?.Barcode,
                    reservation.ConcurrencyToken
                }),
                now);
            await reservations.AddAuditLogAsync(resAudit, cancellationToken);

            var borrowAudit = AuditLog.Create(
                actorId,
                "borrowing.created_from_reservation",
                "Borrowing",
                borrowing.Id,
                null,
                JsonSerializer.Serialize(new
                {
                    borrowing.Id,
                    borrowing.BookId,
                    borrowing.BookCopyId,
                    borrowing.BorrowerId,
                    ReservationId = reservation.Id,
                    borrowing.BorrowedAtUtc,
                    borrowing.DueAtUtc
                }),
                now);
            await reservations.AddAuditLogAsync(borrowAudit, cancellationToken);

            await reservations.SaveChangesAsync(cancellationToken);

            return ReservationResult.Success(ToModel(
                reservation,
                book.Title,
                book.Quantity,
                now,
                0,
                book.Author,
                book.Category,
                member.MemberCode,
                member.MembershipCard?.CardNumber,
                reservation.ConcurrencyToken));
        }
        catch (InvalidOperationException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Conflict, exception.Message);
        }
    }

    private async Task<ReservationModel> MapAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        var member = await members.GetByIdAsync(reservation.ReserverId, cancellationToken);
        var queuePos = await reservations.GetQueuePositionAsync(reservation.BookId, reservation.Id, now, cancellationToken);

        return ToModel(
            reservation,
            book?.Title ?? "Unknown book",
            book?.Quantity ?? 0,
            now,
            queuePos,
            book?.Author,
            book?.Category,
            member?.MemberCode,
            member?.MembershipCard?.CardNumber,
            reservation.ConcurrencyToken);
    }

    private static ReservationModel ToModel(
        Reservation reservation,
        string bookTitle,
        int availableQuantity,
        DateTime utcNow,
        int queuePosition = 0,
        string? bookAuthor = null,
        string? bookCategory = null,
        string? reserverMemberCode = null,
        string? reserverCardNumber = null,
        Guid concurrencyToken = default)
    {
        var status = reservation.IsFulfilled
            ? "fulfilled"
            : reservation.IsCancelled
                ? "cancelled"
                : reservation.IsExpired(utcNow)
                    ? "expired"
                    : availableQuantity > 0
                        ? "ready"
                        : "waiting";

        return new ReservationModel(
            reservation.Id,
            reservation.BookId,
            bookTitle,
            reservation.ReserverId,
            reservation.ReserverName,
            reservation.ReserverEmail,
            reservation.ReservedAtUtc,
            reservation.ExpiresAtUtc,
            reservation.FulfilledAtUtc,
            reservation.CancelledAtUtc,
            status,
            reservation.AppliedPolicyId,
            reservation.AppliedPolicyVersion,
            queuePosition,
            bookAuthor,
            bookCategory,
            reserverMemberCode,
            reserverCardNumber,
            concurrencyToken == default ? reservation.ConcurrencyToken : concurrencyToken);
    }
}
