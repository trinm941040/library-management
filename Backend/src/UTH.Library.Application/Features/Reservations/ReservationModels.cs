namespace UTH.Library.Application.Features.Reservations;

public sealed record ReservationModel(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid ReserverId,
    string ReserverName,
    string ReserverEmail,
    DateTime ReservedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? FulfilledAtUtc,
    DateTime? CancelledAtUtc,
    string Status,
    Guid? AppliedPolicyId,
    int AppliedPolicyVersion,
    int QueuePosition = 0,
    string? BookAuthor = null,
    string? BookCategory = null,
    string? ReserverMemberCode = null,
    string? ReserverCardNumber = null,
    Guid ConcurrencyToken = default);

public sealed record ReservationListQuery(string? Search, string? Status, int PageNumber, int PageSize);

public sealed record ReservationPageModel(
    IReadOnlyList<ReservationModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateReservationCommand(Guid BookId, Guid ReserverId, int HoldDays, Guid? ActorUserId = null);

public sealed record CancelReservationCommand(Guid ActorUserId, string? Reason, Guid ConcurrencyToken);

public sealed record FulfillReservationCommand(Guid ActorUserId, string? BookCopyBarcode, Guid ConcurrencyToken);

public sealed record ReservationDetailModel(
    ReservationModel Reservation,
    string? BookIsbn,
    string? ReserverGroup,
    int AvailableCopiesCount,
    int TotalActiveReservationsForBook,
    string? PolicyName,
    int HoldDays,
    IReadOnlyList<ReservationModel> BookQueue);

public enum ReservationFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record ReservationResult(
    bool Succeeded,
    ReservationFailure Failure,
    ReservationModel? Reservation,
    IReadOnlyList<string> Errors)
{
    public static ReservationResult Success(ReservationModel reservation) =>
        new(true, ReservationFailure.None, reservation, []);

    public static ReservationResult Fail(ReservationFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
