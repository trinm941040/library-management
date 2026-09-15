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
    int AppliedPolicyVersion);

public sealed record ReservationListQuery(string? Search, string? Status, int PageNumber, int PageSize);

public sealed record ReservationPageModel(
    IReadOnlyList<ReservationModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateReservationCommand(Guid BookId, Guid ReserverId, int HoldDays);

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
