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

    public async Task<ReservationResult> CreateAsync(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Book was not found.");

        var reserver = await members.GetByIdAsync(command.ReserverId, cancellationToken);
        if (reserver is null || reserver.Status != MemberStatus.Active)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Reserver was not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (reserver.MembershipCard is null || reserver.MembershipCard.Status != MembershipCardStatus.Active || reserver.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Reserver's membership card is not active.");
        if (reserver.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) && x.Type is MemberRestrictionType.Reservation or MemberRestrictionType.AllTransactions))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Reserver has an active reservation restriction.");

        if (await reservations.HasOpenReservationAsync(command.BookId, command.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "This user already has an open reservation for this book.");

        if (await borrowings.HasActiveBorrowingAsync(command.BookId, command.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "This user already has this book on loan.");

        var policy = await policyResolver.ResolveAsync(reserver.MemberGroup, book.Category, null, now, cancellationToken);
        var holdDays = command.HoldDays <= 0 ? policy.HoldDays : command.HoldDays;

        try
        {
            var reservation = Reservation.Create(
                book.Id,
                reserver.Id,
                reserver.FullName,
                reserver.Email,
                now,
                holdDays);
            await reservations.AddAsync(reservation, cancellationToken);
            await reservations.SaveChangesAsync(cancellationToken);
            return ReservationResult.Success(ToModel(reservation, book.Title, book.Quantity, now));
        }
        catch (ArgumentException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Validation, exception.Message);
        }
    }

    public async Task<ReservationResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Reservation was not found.");

        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            reservation.MarkCancelled(now);
            await reservations.SaveChangesAsync(cancellationToken);
            return ReservationResult.Success(ToModel(reservation, book?.Title ?? "Unknown book", book?.Quantity ?? 0, now));
        }
        catch (InvalidOperationException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Conflict, exception.Message);
        }
    }

    public async Task<ReservationResult> FulfillAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Reservation was not found.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (reservation.IsExpired(now))
            return ReservationResult.Fail(ReservationFailure.Conflict, "Reservation has expired.");

        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        if (book is null)
            return ReservationResult.Fail(ReservationFailure.NotFound, "Book was not found.");
        var member = await members.GetByIdAsync(reservation.ReserverId, cancellationToken);
        if (member is null || member.Status != MemberStatus.Active || member.MembershipCard?.Status != MembershipCardStatus.Active)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Member or membership card is not active.");
        var history = await members.GetHistoryAsync(member.Id, cancellationToken);
        if (history.Borrowings.Count(x => !x.IsReturned) >= member.BorrowingLimit)
            return ReservationResult.Fail(ReservationFailure.Conflict, "Borrowing limit has been reached.");

        if (await borrowings.HasActiveBorrowingAsync(reservation.BookId, reservation.ReserverId, cancellationToken))
            return ReservationResult.Fail(ReservationFailure.Conflict, "This user already has this book on loan.");

        try
        {
            book.Checkout(now);
            reservation.MarkFulfilled(now);
            var borrowing = Borrowing.Create(
                book.Id,
                reservation.ReserverId,
                reservation.ReserverName,
                reservation.ReserverEmail,
                now,
                member.LoanPeriodDays);
            await borrowings.AddAsync(borrowing, cancellationToken);
            await reservations.SaveChangesAsync(cancellationToken);
            return ReservationResult.Success(ToModel(reservation, book.Title, book.Quantity, now));
        }
        catch (InvalidOperationException exception)
        {
            return ReservationResult.Fail(ReservationFailure.Conflict, exception.Message);
        }
    }

    private async Task<ReservationModel> MapAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);
        return ToModel(
            reservation,
            book?.Title ?? "Unknown book",
            book?.Quantity ?? 0,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private static ReservationModel ToModel(Reservation reservation, string bookTitle, int availableQuantity, DateTime utcNow)
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
            status);
    }
}
