using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.CirculationPolicies;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Borrowings;

public sealed class BorrowingService(
    IBorrowingRepository borrowings,
    IBookRepository books,
    IMemberRepository members,
    ICirculationPolicyResolver policyResolver,
    TimeProvider timeProvider,
    IViolationRepository violations,
    IReservationRepository reservations)
{
    public async Task<BorrowingPageModel> GetAsync(BorrowingListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (items, totalCount) = await borrowings.GetPageAsync(
            query.Search,
            query.Status,
            pageNumber,
            pageSize,
            now,
            cancellationToken);

        var models = new List<BorrowingModel>(items.Count);
        foreach (var item in items)
            models.Add(await MapAsync(item, cancellationToken));

        return new BorrowingPageModel(models, pageNumber, pageSize, totalCount);
    }

    public async Task<BorrowingModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        return borrowing is null ? null : await MapAsync(borrowing, cancellationToken);
    }

    public async Task<MemberCheckoutLookupResult?> LookupMemberForCheckoutAsync(string cardOrCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cardOrCode)) return null;
        var member = await members.GetByCardOrCodeAsync(cardOrCode, cancellationToken);
        if (member is null) return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var reasons = new List<string>();

        if (member.Status != MemberStatus.Active)
            reasons.Add($"Tài khoản độc giả hiện đang ở trạng thái '{member.Status}' (không thể mượn sách).");

        if (member.MembershipCard is null)
            reasons.Add("Độc giả chưa được cấp thẻ thư viện.");
        else
        {
            if (member.MembershipCard.Status != MembershipCardStatus.Active)
                reasons.Add($"Thẻ thư viện ở trạng thái '{member.MembershipCard.Status}' (không hoạt động).");
            if (member.MembershipCard.ExpiresOn < today)
                reasons.Add($"Thẻ thư viện đã hết hạn vào ngày {member.MembershipCard.ExpiresOn:dd/MM/yyyy}.");
        }

        var activeRestrictions = member.Restrictions
            .Where(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) &&
                        x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions)
            .ToList();
        foreach (var r in activeRestrictions)
            reasons.Add($"Đang có lệnh hạn chế mượn sách: {r.Reason}");

        var history = await members.GetHistoryAsync(member.Id, cancellationToken);
        var activeLoans = history.Borrowings.Count(x => !x.IsReturned);
        var overdueLoans = history.Borrowings.Count(x => x.IsOverdue(now));

        if (activeLoans >= member.BorrowingLimit)
            reasons.Add($"Đã đạt hạn mức mượn sách tối đa ({activeLoans}/{member.BorrowingLimit} cuốn).");

        if (overdueLoans > 0)
            reasons.Add($"Đang có {overdueLoans} cuốn sách mượn quá hạn chưa hoàn trả.");

        return new MemberCheckoutLookupResult(
            member.Id,
            member.MemberCode,
            member.FullName,
            member.Email,
            member.MemberGroup,
            member.MembershipCard?.CardNumber,
            member.MembershipCard?.ExpiresOn,
            member.MembershipCard?.Status.ToString(),
            member.Status.ToString(),
            activeLoans,
            member.BorrowingLimit,
            overdueLoans,
            reasons.Count == 0,
            reasons);
    }

    public async Task<BookCopyCheckoutLookupResult?> LookupBookCopyForCheckoutAsync(string barcode, Guid? memberId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var copy = await borrowings.GetBookCopyByBarcodeAsync(barcode, cancellationToken);
        if (copy is null) return null;

        var book = await books.GetByIdAsync(copy.BookId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasActiveBorrowing = await borrowings.HasActiveBorrowingForCopyAsync(copy.Id, cancellationToken);

        var isAvailable = copy.Status == CopyStatus.Available && !hasActiveBorrowing;
        string? ineligibilityReason = null;
        if (!isAvailable)
        {
            if (hasActiveBorrowing || copy.Status == CopyStatus.Borrowed)
                ineligibilityReason = "Bản sao này hiện đang được mượn bởi độc giả khác.";
            else if (copy.Status == CopyStatus.Damaged)
                ineligibilityReason = "Bản sao này đang bị đánh dấu hư hỏng.";
            else if (copy.Status == CopyStatus.Lost)
                ineligibilityReason = "Bản sao này đang bị đánh dấu đã mất.";
            else if (copy.Status == CopyStatus.Withdrawn)
                ineligibilityReason = "Bản sao này đã thanh lý hoặc rút khỏi lưu hành.";
            else
                ineligibilityReason = $"Bản sao sách không khả dụng (Trạng thái: {copy.Status}).";
        }

        string? memberGroup = null;
        int memberLoanDays = 14;
        if (memberId.HasValue)
        {
            var member = await members.GetByIdAsync(memberId.Value, cancellationToken);
            if (member is not null)
            {
                memberGroup = member.MemberGroup;
                memberLoanDays = member.LoanPeriodDays;
            }
        }

        var policy = await policyResolver.ResolveAsync(memberGroup, book?.Category ?? "All", null, now, cancellationToken);
        var loanDays = Math.Min(memberLoanDays, policy.LoanPeriodDays);

        return new BookCopyCheckoutLookupResult(
            copy.Id,
            copy.BookId,
            copy.Barcode,
            book?.Title ?? "Không rõ tên sách",
            book?.Author ?? "Không rõ tác giả",
            book?.Isbn ?? string.Empty,
            book?.Category ?? string.Empty,
            copy.Condition.ToString(),
            copy.Status.ToString(),
            isAvailable,
            ineligibilityReason,
            policy.PolicyName,
            loanDays,
            now.AddDays(loanDays));
    }

    public async Task<BorrowingResult> CheckoutWithBarcodeAsync(
        CheckoutWithBarcodeCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.MemberCardOrCode))
            return BorrowingResult.Fail(BorrowingFailure.Validation, "Mã thẻ hoặc mã độc giả là bắt buộc.");
        if (string.IsNullOrWhiteSpace(command.BookBarcode))
            return BorrowingResult.Fail(BorrowingFailure.Validation, "Mã vạch bản sao sách là bắt buộc.");

        var member = await members.GetByCardOrCodeAsync(command.MemberCardOrCode, cancellationToken);
        if (member is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Không tìm thấy độc giả tương ứng với mã thẻ hoặc mã độc giả đã quét.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        if (member.Status != MemberStatus.Active)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Tài khoản độc giả đang ở trạng thái '{member.Status}', không thể lập phiếu mượn.");

        if (member.MembershipCard is null)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Độc giả chưa được phát hành thẻ thư viện.");

        if (member.MembershipCard.Status != MembershipCardStatus.Active)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Thẻ thư viện của độc giả đang ở trạng thái '{member.MembershipCard.Status}'.");

        if (member.MembershipCard.ExpiresOn < today)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Thẻ thư viện của độc giả đã hết hạn vào ngày {member.MembershipCard.ExpiresOn:dd/MM/yyyy}.");

        if (member.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) &&
                                         x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Độc giả đang có lệnh hạn chế quyền mượn sách.");

        var copy = await borrowings.GetBookCopyByBarcodeAsync(command.BookBarcode, cancellationToken);
        if (copy is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Không tìm thấy bản sao sách với mã vạch đã quét.");

        if (copy.Status != CopyStatus.Available)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Bản sao sách '{copy.Barcode}' không khả dụng (Trạng thái: {copy.Status}).");

        if (await borrowings.HasActiveBorrowingForCopyAsync(copy.Id, cancellationToken))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Bản sao sách '{copy.Barcode}' hiện đang có phiếu mượn chưa hoàn tất.");

        var book = await books.GetByIdAsync(copy.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Không tìm thấy đầu sách tương ứng của bản sao.");

        if (await borrowings.HasActiveBorrowingAsync(book.Id, member.Id, cancellationToken))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Độc giả này hiện đang mượn một bản sao khác của cùng đầu sách.");

        var history = await members.GetHistoryAsync(member.Id, cancellationToken);
        var policy = await policyResolver.ResolveAsync(member.MemberGroup, book.Category, null, now, cancellationToken);
        var maxLoans = Math.Min(member.BorrowingLimit, policy.MaxLoanBooks);

        if (history.Borrowings.Count(x => !x.IsReturned) >= maxLoans)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Độc giả đã đạt giới hạn mượn sách cho phép ({maxLoans} cuốn).");

        if (policy.BlockIfOverdue && history.Borrowings.Any(x => x.IsOverdue(now)))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Độc giả đang có sách quá hạn chưa trả, chính sách lưu thông từ chối cho mượn tiếp.");

        try
        {
            var maximumLoanDays = Math.Min(member.LoanPeriodDays, policy.LoanPeriodDays);
            var loanDays = command.LoanDaysOverride is > 0 ? Math.Min(command.LoanDaysOverride.Value, maximumLoanDays) : maximumLoanDays;

            copy.Checkout(now);
            book.Checkout(now);

            var borrowing = Borrowing.CreateWithCopy(
                book.Id,
                copy.Id,
                member.Id,
                member.FullName,
                member.Email,
                now,
                loanDays,
                actorUserId,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));

            await borrowings.AddAsync(borrowing, cancellationToken);
            await borrowings.SaveChangesAsync(cancellationToken);

            return BorrowingResult.Success(ToModel(borrowing, book.Title, now, copy.Barcode));
        }
        catch (OptimisticConcurrencyException)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Bản sao sách vừa được cập nhật bởi yêu cầu khác. Vui lòng kiểm tra lại.");
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Validation, exception.Message);
        }
    }

    public async Task<BorrowingResult> CreateAsync(CreateBorrowingCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Book was not found.");

        var borrower = await members.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null || borrower.Status != MemberStatus.Active)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrower was not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (borrower.MembershipCard is null || borrower.MembershipCard.Status != MembershipCardStatus.Active || borrower.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrower's membership card is not active.");
        if (borrower.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) && x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrower has an active borrowing restriction.");
        var history = await members.GetHistoryAsync(borrower.Id, cancellationToken);
        var policy = await policyResolver.ResolveAsync(borrower.MemberGroup, book.Category, null, now, cancellationToken);
        var maxLoans = Math.Min(borrower.BorrowingLimit, policy.MaxLoanBooks);
        if (history.Borrowings.Count(x => !x.IsReturned) >= maxLoans)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrowing limit has been reached.");
        if (policy.BlockIfOverdue && history.Borrowings.Any(x => x.IsOverdue(now)))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrower has an overdue loan.");

        if (await borrowings.HasActiveBorrowingAsync(command.BookId, command.BorrowerId, cancellationToken))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "This borrower already has this book on loan.");

        try
        {
            var maximumLoanDays = Math.Min(borrower.LoanPeriodDays, policy.LoanPeriodDays);
            var loanDays = command.LoanDays <= 0 ? maximumLoanDays : command.LoanDays;
            if (loanDays > maximumLoanDays)
                return BorrowingResult.Fail(BorrowingFailure.Validation, "Loan period exceeds the applicable policy.");
            book.Checkout(now);
            var borrowing = Borrowing.Create(
                book.Id,
                borrower.Id,
                borrower.FullName,
                borrower.Email,
                now,
                loanDays,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));
            await borrowings.AddAsync(borrowing, cancellationToken);
            await borrowings.SaveChangesAsync(cancellationToken);
            return BorrowingResult.Success(ToModel(borrowing, book.Title, now));
        }
        catch (OptimisticConcurrencyException)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Dữ liệu vừa được cập nhật bởi yêu cầu khác.");
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Validation, exception.Message);
        }
    }

    public async Task<RenewalPreviewResult?> GetRenewalPreviewAsync(Guid borrowingId, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing is null) return null;

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        var borrower = await members.GetByIdAsync(borrowing.BorrowerId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var policy = await policyResolver.ResolveAsync(borrower?.MemberGroup, book?.Category, null, now, cancellationToken);
        var reasons = new List<string>();

        if (borrowing.IsReturned)
            reasons.Add("Khoản mượn đã hoàn tất, không thể gia hạn.");

        if (borrower is null || borrower.Status != MemberStatus.Active)
            reasons.Add("Tài khoản độc giả hiện không hoạt động hoặc không tồn tại.");

        if (borrower?.MembershipCard is null || borrower.MembershipCard.Status != MembershipCardStatus.Active ||
            borrower.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            reasons.Add("Thẻ thư viện của độc giả không còn hiệu lực.");

        if (borrower?.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) &&
                                            x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions) == true)
            reasons.Add("Độc giả đang có lệnh hạn chế quyền mượn/gia hạn sách.");

        if (policy.BlockIfOverdue && borrowing.IsOverdue(now))
            reasons.Add("Khoản mượn đã quá hạn, chính sách lưu thông từ chối gia hạn.");

        if (borrowing.RenewalCount >= policy.MaxRenewals)
            reasons.Add($"Đã đạt giới hạn gia hạn tối đa ({borrowing.RenewalCount}/{policy.MaxRenewals} lần).");

        var waitingReservation = await reservations.GetFirstWaitingReservationForBookAsync(borrowing.BookId, now, cancellationToken);
        if (waitingReservation is not null && waitingReservation.ReserverId != borrowing.BorrowerId)
            reasons.Add("Tác phẩm này đang có độc giả khác đặt trước, ưu tiên giao sách cho người đặt trước.");

        var renewals = await borrowings.GetRenewalsByBorrowingIdAsync(borrowing.Id, cancellationToken);
        var history = renewals.Select(r => new RenewalHistoryModel(
            r.Id,
            r.BorrowingId,
            r.PreviousDueAtUtc,
            r.NewDueAtUtc,
            r.RenewedByUserId,
            null,
            r.RenewedAtUtc,
            r.AppliedPolicyId,
            r.AppliedPolicyVersion)).ToList();

        var renewalDays = policy.RenewalPeriodDays > 0 ? policy.RenewalPeriodDays : 14;
        var proposedDue = borrowing.DueAtUtc.AddDays(renewalDays);

        return new RenewalPreviewResult(
            borrowing.Id,
            borrowing.BookId,
            book?.Title ?? "Tác phẩm",
            borrowing.BorrowerId,
            borrowing.BorrowerName,
            borrowing.DueAtUtc,
            proposedDue,
            borrowing.RenewalCount,
            policy.MaxRenewals,
            renewalDays,
            reasons.Count == 0,
            reasons,
            policy.PolicyName,
            borrowing.ConcurrencyToken,
            history);
    }

    public async Task<BorrowingDetailModel?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        if (borrowing is null) return null;

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        var borrower = await members.GetByIdAsync(borrowing.BorrowerId, cancellationToken);
        var preview = await GetRenewalPreviewAsync(id, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        string? barcode = null;
        if (borrowing.BookCopyId.HasValue)
        {
            var copy = await borrowings.GetBookCopyByIdAsync(borrowing.BookCopyId.Value, cancellationToken);
            barcode = copy?.Barcode;
        }

        var model = ToModel(borrowing, book?.Title ?? "Không rõ tên sách", now, barcode);

        return new BorrowingDetailModel(
            model,
            book?.Author,
            book?.Isbn,
            book?.Category,
            borrower?.MemberCode,
            borrower?.MembershipCard?.CardNumber,
            borrower?.MemberGroup,
            preview?.History ?? [],
            preview!);
    }

    public async Task<BorrowingResult> RenewAsync(Guid id, RenewBorrowingCommand command, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        if (borrowing is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrowing was not found.");

        if (borrowing.IsReturned)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Khoản mượn đã hoàn tất, không thể gia hạn.");

        if (borrowing.ConcurrencyToken != command.ConcurrencyToken)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Phiếu mượn đã bị thay đổi bởi thao tác khác (Xung đột Concurrency). Vui lòng tải lại trang.");

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Book was not found.");

        var borrower = await members.GetByIdAsync(borrowing.BorrowerId, cancellationToken);
        if (borrower is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrower was not found.");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (borrower.Status != MemberStatus.Active)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Tài khoản độc giả đang ở trạng thái '{borrower.Status}', không thể gia hạn.");

        if (borrower.MembershipCard is null || borrower.MembershipCard.Status != MembershipCardStatus.Active ||
            borrower.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Thẻ thư viện của độc giả không còn hiệu lực.");

        if (borrower.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) &&
                                           x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Độc giả đang có lệnh hạn chế quyền mượn/gia hạn sách.");

        var waitingReservation = await reservations.GetFirstWaitingReservationForBookAsync(borrowing.BookId, now, cancellationToken);
        if (waitingReservation is not null && waitingReservation.ReserverId != borrowing.BorrowerId)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Tác phẩm này đang có độc giả khác đặt trước, ưu tiên giao sách cho người đặt trước.");

        var policy = await policyResolver.ResolveAsync(borrower.MemberGroup, book.Category, null, now, cancellationToken);

        if (policy.BlockIfOverdue && borrowing.IsOverdue(now))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Khoản mượn đã quá hạn, chính sách lưu thông từ chối cho phép gia hạn.");

        if (borrowing.RenewalCount >= policy.MaxRenewals)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, $"Khoản mượn đã đạt số lần gia hạn tối đa ({policy.MaxRenewals} lần).");

        try
        {
            var previousDueAtUtc = borrowing.DueAtUtc;
            var beforeJson = JsonSerializer.Serialize(new { borrowing.DueAtUtc, borrowing.RenewalCount, borrowing.ConcurrencyToken });
            var renewalDays = policy.RenewalPeriodDays > 0 ? policy.RenewalPeriodDays : 14;
            borrowing.Renew(renewalDays, policy.MaxRenewals);

            var renewal = Renewal.Create(
                borrowing.Id,
                previousDueAtUtc,
                borrowing.DueAtUtc,
                command.ActorUserId,
                now,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));

            await borrowings.AddRenewalAsync(renewal, cancellationToken);
            var afterJson = JsonSerializer.Serialize(new { borrowing.DueAtUtc, borrowing.RenewalCount, borrowing.ConcurrencyToken, RenewalId = renewal.Id });
            await borrowings.AddAuditLogAsync(AuditLog.Create(
                command.ActorUserId, "borrowing.renewed", nameof(Borrowing), borrowing.Id,
                beforeJson, afterJson, now), cancellationToken);
            await borrowings.SaveChangesAsync(cancellationToken);

            string? copyBarcode = null;
            if (borrowing.BookCopyId.HasValue)
            {
                var copy = await borrowings.GetBookCopyByIdAsync(borrowing.BookCopyId.Value, cancellationToken);
                copyBarcode = copy?.Barcode;
            }

            return BorrowingResult.Success(ToModel(borrowing, book.Title, now, copyBarcode));
        }
        catch (OptimisticConcurrencyException)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Phiếu mượn đã bị thay đổi bởi thao tác khác.");
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Validation, exception.Message);
        }
    }

    public async Task<BorrowingResult> ReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        if (borrowing is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrowing was not found.");

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Book was not found.");

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            borrowing.MarkReturned(now);
            book.CheckIn(now);

            if (borrowing.BookCopyId.HasValue)
            {
                var copy = await borrowings.GetBookCopyByIdAsync(borrowing.BookCopyId.Value, cancellationToken);
                copy?.Return(now);
            }

            await borrowings.SaveChangesAsync(cancellationToken);
            return BorrowingResult.Success(ToModel(borrowing, book.Title, now));
        }
        catch (OptimisticConcurrencyException)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Dữ liệu bị xung đột cập nhật.");
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
    }

    public async Task<BookCopyReturnLookupResult?> LookupCopyForReturnAsync(string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var copy = await borrowings.GetBookCopyByBarcodeAsync(barcode, cancellationToken);
        if (copy is null) return null;

        var borrowing = await borrowings.GetActiveBorrowingByCopyIdAsync(copy.Id, cancellationToken);
        if (borrowing is null) return null;

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        var borrower = await members.GetByIdAsync(borrowing.BorrowerId, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        ResolvedCirculationPolicy? policy = null;
        if (!string.IsNullOrWhiteSpace(borrowing.AppliedPolicySnapshot) && borrowing.AppliedPolicySnapshot != "{}")
        {
            try
            {
                policy = JsonSerializer.Deserialize<ResolvedCirculationPolicy>(borrowing.AppliedPolicySnapshot);
            }
            catch
            {
                // ignore
            }
        }

        policy ??= await policyResolver.ResolveAsync(borrower?.MemberGroup, book?.Category, null, now, cancellationToken);

        var isOverdue = borrowing.IsOverdue(now) || (now > borrowing.DueAtUtc);
        var overdueDays = isOverdue ? Math.Max(1, (int)Math.Ceiling((now - borrowing.DueAtUtc).TotalDays)) : 0;
        var finePerDay = policy.FinePerDay > 0 ? policy.FinePerDay : 5000m;
        var maxFine = policy.MaxFineAmount > 0 ? policy.MaxFineAmount : 100000m;
        var estimatedOverdueFine = isOverdue ? Math.Min(overdueDays * finePerDay, maxFine) : 0m;
        var fixedDamageFine = policy.FixedFineAmount > 0 ? policy.FixedFineAmount : 50000m;
        var penaltyRatio = policy.LostBookPenaltyRatio > 0 ? policy.LostBookPenaltyRatio : 150m;
        var estimatedLostFine = 100000m * (penaltyRatio / 100m);

        return new BookCopyReturnLookupResult(
            borrowing.Id,
            book?.Id ?? borrowing.BookId,
            book?.Title ?? "Tác phẩm",
            book?.Author ?? "Tác giả",
            copy.Id,
            copy.Barcode,
            copy.Condition.ToString(),
            borrowing.BorrowerId,
            borrowing.BorrowerName,
            borrowing.BorrowerEmail,
            borrower?.MemberCode,
            borrower?.MembershipCard?.CardNumber,
            borrowing.BorrowedAtUtc,
            borrowing.DueAtUtc,
            isOverdue,
            overdueDays,
            finePerDay,
            estimatedOverdueFine,
            fixedDamageFine,
            estimatedLostFine,
            policy.PolicyName,
            borrowing.ConcurrencyToken);
    }

    public async Task<ReturnResult> ConfirmReturnAsync(ConfirmReturnCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Barcode))
            return ReturnResult.Fail(BorrowingFailure.Validation, "Mã vạch bản sao sách là bắt buộc.");

        var parsedCondition = Enum.TryParse<CopyCondition>(command.Condition, true, out var c)
            ? c
            : CopyCondition.Good;

        if (parsedCondition is CopyCondition.Damaged or CopyCondition.Lost && string.IsNullOrWhiteSpace(command.Note))
            return ReturnResult.Fail(BorrowingFailure.Validation, "Ghi chú là bắt buộc khi sách bị hư hỏng hoặc mất.");

        var copy = await borrowings.GetBookCopyByBarcodeAsync(command.Barcode, cancellationToken);
        if (copy is null)
            return ReturnResult.Fail(BorrowingFailure.NotFound, "Không tìm thấy bản sao sách với mã vạch đã cung cấp.");

        var borrowing = await borrowings.GetActiveBorrowingByCopyIdAsync(copy.Id, cancellationToken);
        if (borrowing is null || borrowing.IsReturned)
            return ReturnResult.Fail(BorrowingFailure.Conflict, "Bản sao này hiện không có khoản mượn nào cần trả hoặc đã được hoàn trả trước đó.");

        if (command.ConcurrencyToken != Guid.Empty && borrowing.ConcurrencyToken != command.ConcurrencyToken)
            return ReturnResult.Fail(BorrowingFailure.Conflict, "Khoản mượn vừa được cập nhật bởi yêu cầu khác. Vui lòng thử lại.");

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        var borrower = await members.GetByIdAsync(borrowing.BorrowerId, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        ResolvedCirculationPolicy? policy = null;
        if (!string.IsNullOrWhiteSpace(borrowing.AppliedPolicySnapshot) && borrowing.AppliedPolicySnapshot != "{}")
        {
            try
            {
                policy = JsonSerializer.Deserialize<ResolvedCirculationPolicy>(borrowing.AppliedPolicySnapshot);
            }
            catch
            {
                // ignore
            }
        }
        policy ??= await policyResolver.ResolveAsync(borrower?.MemberGroup, book?.Category, null, now, cancellationToken);

        try
        {
            borrowing.MarkReturned(now);

            var waitingReservation = await reservations.GetFirstWaitingReservationForBookAsync(borrowing.BookId, now, cancellationToken);
            var hasWaitingReservation = waitingReservation != null;

            if (parsedCondition == CopyCondition.Damaged)
            {
                copy.ReturnWithCondition(CopyCondition.Damaged, CopyStatus.Damaged, now);
            }
            else if (parsedCondition == CopyCondition.Lost)
            {
                copy.ReturnWithCondition(CopyCondition.Lost, CopyStatus.Lost, now);
            }
            else
            {
                book?.CheckIn(now);
                var targetStatus = hasWaitingReservation ? CopyStatus.Reserved : CopyStatus.Available;
                copy.ReturnWithCondition(parsedCondition, targetStatus, now);
            }

            var createdViolations = new List<ViolationSummaryModel>();
            var totalFine = 0m;

            var isOverdue = borrowing.IsOverdue(now) || (now > borrowing.DueAtUtc);
            if (isOverdue)
            {
                var overdueDays = Math.Max(1, (int)Math.Ceiling((now - borrowing.DueAtUtc).TotalDays));
                var finePerDay = policy.FinePerDay > 0 ? policy.FinePerDay : 5000m;
                var maxFine = policy.MaxFineAmount > 0 ? policy.MaxFineAmount : 100000m;
                var overdueFine = Math.Min(overdueDays * finePerDay, maxFine);
                if (overdueFine > 0)
                {
                    var overdueViolation = Violation.Create(
                        borrowing.BorrowerId,
                        borrowing.BorrowerName,
                        borrowing.BorrowerEmail,
                        book?.Id ?? borrowing.BookId,
                        book?.Title ?? "Tác phẩm",
                        "overdue",
                        $"Quá hạn {overdueDays} ngày (Hạn trả: {borrowing.DueAtUtc:dd/MM/yyyy HH:mm}).",
                        overdueFine,
                        now,
                        policy.PolicyId,
                        policy.Version,
                        borrowing.AppliedPolicySnapshot);
                    await violations.AddAsync(overdueViolation, cancellationToken);
                    createdViolations.Add(new ViolationSummaryModel(
                        overdueViolation.Id,
                        overdueViolation.Type,
                        overdueViolation.Note,
                        overdueViolation.FineAmount,
                        overdueViolation.RecordedAtUtc));
                    totalFine += overdueFine;
                }
            }

            if (parsedCondition == CopyCondition.Damaged)
            {
                var damageFine = command.CustomDamageFine ?? (policy.FixedFineAmount > 0 ? policy.FixedFineAmount : 50000m);
                var damageViolation = Violation.Create(
                    borrowing.BorrowerId,
                    borrowing.BorrowerName,
                    borrowing.BorrowerEmail,
                    book?.Id ?? borrowing.BookId,
                    book?.Title ?? "Tác phẩm",
                    "damage",
                    $"Sách bị hư hỏng khi trả. Ghi chú: {command.Note?.Trim()}",
                    damageFine,
                    now,
                    policy.PolicyId,
                    policy.Version,
                    borrowing.AppliedPolicySnapshot);
                await violations.AddAsync(damageViolation, cancellationToken);
                createdViolations.Add(new ViolationSummaryModel(
                    damageViolation.Id,
                    damageViolation.Type,
                    damageViolation.Note,
                    damageViolation.FineAmount,
                    damageViolation.RecordedAtUtc));
                totalFine += damageFine;
            }
            else if (parsedCondition == CopyCondition.Lost)
            {
                var penaltyRatio = policy.LostBookPenaltyRatio > 0 ? policy.LostBookPenaltyRatio : 150m;
                var lostFine = command.CustomLostFine ?? (100000m * (penaltyRatio / 100m));
                var lostViolation = Violation.Create(
                    borrowing.BorrowerId,
                    borrowing.BorrowerName,
                    borrowing.BorrowerEmail,
                    book?.Id ?? borrowing.BookId,
                    book?.Title ?? "Tác phẩm",
                    "lost",
                    $"Báo mất sách khi trả. Ghi chú: {command.Note?.Trim()}",
                    lostFine,
                    now,
                    policy.PolicyId,
                    policy.Version,
                    borrowing.AppliedPolicySnapshot);
                await violations.AddAsync(lostViolation, cancellationToken);
                createdViolations.Add(new ViolationSummaryModel(
                    lostViolation.Id,
                    lostViolation.Type,
                    lostViolation.Note,
                    lostViolation.FineAmount,
                    lostViolation.RecordedAtUtc));
                totalFine += lostFine;
            }

            await borrowings.SaveChangesAsync(cancellationToken);

            var borrowingModel = ToModel(borrowing, book?.Title ?? "Tác phẩm", now, copy.Barcode);
            return ReturnResult.Success(new ReturnExecutionResult(
                borrowingModel,
                now,
                parsedCondition.ToString(),
                copy.Status.ToString(),
                createdViolations,
                totalFine,
                hasWaitingReservation));
        }
        catch (OptimisticConcurrencyException)
        {
            return ReturnResult.Fail(BorrowingFailure.Conflict, "Dữ liệu bị xung đột cập nhật.");
        }
        catch (InvalidOperationException exception)
        {
            return ReturnResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ReturnResult.Fail(BorrowingFailure.Validation, exception.Message);
        }
    }


    private async Task<BorrowingModel> MapAsync(Borrowing borrowing, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        string? barcode = null;
        if (borrowing.BookCopyId.HasValue)
        {
            var copy = await borrowings.GetBookCopyByIdAsync(borrowing.BookCopyId.Value, cancellationToken);
            barcode = copy?.Barcode;
        }

        return ToModel(borrowing, book?.Title ?? "Unknown book", timeProvider.GetUtcNow().UtcDateTime, barcode);
    }

    private static BorrowingModel ToModel(Borrowing borrowing, string bookTitle, DateTime utcNow, string? copyBarcode = null)
    {
        var status = borrowing.IsReturned
            ? "returned"
            : borrowing.IsOverdue(utcNow)
                ? "overdue"
                : "borrowed";

        return new BorrowingModel(
            borrowing.Id,
            borrowing.BookId,
            bookTitle,
            borrowing.BorrowerId,
            borrowing.BorrowerName,
            borrowing.BorrowerEmail,
            borrowing.BorrowedAtUtc,
            borrowing.DueAtUtc,
            borrowing.ReturnedAtUtc,
            status,
            borrowing.RenewalCount,
            borrowing.AppliedPolicyId,
            borrowing.AppliedPolicyVersion,
            borrowing.BookCopyId,
            copyBarcode,
            borrowing.ProcessedByEmployeeId);
    }
}
