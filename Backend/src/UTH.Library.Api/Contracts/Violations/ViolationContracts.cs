using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Violations;

public sealed class ViolationFilterRequest
{
    public string? Search { get; init; }
    public Guid? BorrowerId { get; init; }
    public string? Type { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool? HasBalanceOnly { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateViolationRequest(
    [Required] Guid BorrowerId,
    Guid? BookId,
    Guid? BookCopyId,
    Guid? BorrowingId,
    [Required, StringLength(20, MinimumLength = 1)] string Type,
    [StringLength(500)] string Note,
    [Range(0, 100_000_000)] decimal FineAmount,
    [Range(0, 10000)] int OverdueDays = 0,
    [Range(0, 1_000_000_000)] decimal BookPrice = 0,
    string? DamageLevel = null);

public sealed record FinePreviewRequest(
    [Required] Guid BorrowerId,
    Guid? BookId,
    [Required] string Type,
    int OverdueDays = 0,
    decimal BookPrice = 0,
    string? DamageLevel = null,
    decimal? CustomAmount = null);

public sealed record FinePreviewResponse(
    decimal CalculatedFine,
    string Formula,
    string? PolicyName,
    decimal DailyRate,
    decimal MaxFine,
    decimal LostRatio,
    Guid? PolicyId,
    int PolicyVersion);

public sealed record PaymentHistoryItemResponse(
    Guid Id,
    decimal Amount,
    string Method,
    string Reference,
    DateTime PaidAtUtc,
    Guid? ReceivedByUserId);

public sealed record AdjustmentHistoryItemResponse(
    Guid Id,
    decimal AmountDelta,
    string Reason,
    DateTime AdjustedAtUtc,
    Guid? AdjustedByUserId);

public sealed record ViolationResponse(
    Guid Id,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    Guid? BookId,
    string BookTitle,
    string Type,
    string Note,
    decimal FineAmount,
    DateTime RecordedAtUtc,
    DateTime? ResolvedAtUtc,
    string Status,
    Guid? AppliedPolicyId,
    int AppliedPolicyVersion,
    decimal TotalAdjusted = 0m,
    decimal TotalPaid = 0m,
    decimal Balance = 0m,
    Guid? BorrowingId = null,
    Guid? BookCopyId = null,
    string? BookCopyBarcode = null,
    string? BorrowerMemberCode = null,
    string? BorrowerCardNumber = null,
    Guid ConcurrencyToken = default);

public sealed record ViolationPageResponse(
    IReadOnlyCollection<ViolationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ViolationDetailResponse(
    ViolationResponse Violation,
    string? BookIsbn,
    string? BookAuthor,
    string? BookCategory,
    string? CalculationBasis,
    IReadOnlyList<PaymentHistoryItemResponse> Payments,
    IReadOnlyList<AdjustmentHistoryItemResponse> Adjustments);

public sealed record PayViolationRequest(
    [Range(0.01, 100_000_000)] decimal? Amount = null,
    UTH.Library.Domain.Entities.FinePaymentMethod? Method = null,
    [StringLength(200)] string? Reference = null,
    [StringLength(100)] string? IdempotencyKey = null);

