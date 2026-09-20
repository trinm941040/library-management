using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Reservations;

public sealed class ReservationFilterRequest
{
    [StringLength(200)] public string? Search { get; init; }
    [RegularExpression("^(waiting|ready|expired|fulfilled|cancelled)$")] public string? Status { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateReservationRequest(
    [Required] Guid BookId,
    [Required] Guid ReserverId,
    [Range(0, 365)] int HoldDays = 0);

public sealed record CancelReservationRequest(
    string? Reason,
    [Required] Guid ConcurrencyToken);

public sealed record FulfillReservationRequest(
    string? BookCopyBarcode,
    [Required] Guid ConcurrencyToken);

public sealed record ReservationResponse(
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

public sealed record ReservationPageResponse(
    IReadOnlyCollection<ReservationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ReservationDetailResponse(
    ReservationResponse Reservation,
    string? BookIsbn,
    string? ReserverGroup,
    int AvailableCopiesCount,
    int TotalActiveReservationsForBook,
    string? PolicyName,
    int HoldDays,
    IReadOnlyList<ReservationResponse> BookQueue);
