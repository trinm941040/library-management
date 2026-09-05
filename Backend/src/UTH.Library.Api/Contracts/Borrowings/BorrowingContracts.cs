using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Borrowings;

public sealed class BorrowingFilterRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateBorrowingRequest(
    [Required] Guid BookId,
    [Required] Guid BorrowerId,
    [Range(1, 365)] int LoanDays = 14);

public sealed record BorrowingResponse(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    string Status);

public sealed record BorrowingPageResponse(
    IReadOnlyCollection<BorrowingResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
