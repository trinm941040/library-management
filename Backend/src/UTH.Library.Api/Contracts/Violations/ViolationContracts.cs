using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Violations;

public sealed class ViolationFilterRequest
{
    [StringLength(200)] public string? Search { get; init; }
    [RegularExpression("^(open|paid|waived)$")] public string? Status { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateViolationRequest(
    [Required] Guid BorrowerId,
    Guid? BookId,
    [Required, StringLength(20, MinimumLength = 1)] string Type,
    [StringLength(500)] string Note,
    [Range(0, 100_000_000)] decimal FineAmount,
    [Range(0, 10000)] int OverdueDays = 0,
    [Range(0, 1_000_000_000)] decimal BookPrice = 0);

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
    int AppliedPolicyVersion);

public sealed record ViolationPageResponse(
    IReadOnlyCollection<ViolationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
