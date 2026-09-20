using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Api.Contracts.Payments;

public sealed class PaymentFilterRequest
{
    public string? Search { get; init; }
    public Guid? ViolationId { get; init; }
    public Guid? MemberId { get; init; }
    public FinePaymentMethod? Method { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateFinePaymentRequest(
    [Required] Guid ViolationId,
    [Range(0.01, 100_000_000)] decimal Amount,
    [Required] FinePaymentMethod Method,
    [StringLength(200)] string? Reference,
    [StringLength(100)] string? IdempotencyKey);

public sealed record FinePaymentPreviewRequest(
    [Required] Guid ViolationId);

public sealed record FinePaymentPreviewResponse(
    Guid ViolationId,
    string ViolationType,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    string? MemberCode,
    string BorrowerEmail,
    decimal FineAmount,
    decimal TotalAdjusted,
    decimal TotalPaid,
    decimal Balance,
    decimal SuggestedAmount,
    bool IsOpen,
    string Status);

public sealed record FinePaymentReceiptResponse(
    Guid Id,
    Guid ViolationId,
    string ViolationType,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    string? MemberCode,
    decimal Amount,
    decimal PreviousBalance,
    decimal RemainingBalance,
    string Method,
    string Reference,
    DateTime PaidAtUtc,
    Guid? ReceivedByUserId,
    string? ReceivedByUserName,
    bool IsFullyPaid);

public sealed record FinePaymentPageResponse(
    IReadOnlyCollection<FinePaymentReceiptResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
