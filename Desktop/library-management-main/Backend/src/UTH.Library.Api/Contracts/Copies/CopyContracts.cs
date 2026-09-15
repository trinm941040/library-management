using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Copies;

public sealed class CopyFilterRequest
{
    public string? Search { get; init; }
    public Guid? BookId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? ShelfId { get; init; }
    public CopyCondition? Condition { get; init; }
    public CopyStatus? Status { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record CreateCopyRequest(
    Guid BookId,
    [Required, StringLength(64)] string Barcode,
    CopyCondition Condition,
    Guid? ShelfId = null,
    Guid? StockReceiptItemId = null);

public sealed record UpdateCopyStatusRequest(CopyStatus Status, Guid? ConcurrencyToken = null);
public sealed record RelocateCopyRequest(Guid ShelfId, Guid? ConcurrencyToken = null);

public sealed record CopyResponse(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string Barcode,
    CopyCondition Condition,
    CopyStatus Status,
    DateTime AcquiredAtUtc,
    Guid? ShelfId,
    string? ShelfCode,
    Guid? BranchId,
    string? BranchCode,
    Guid? StockReceiptItemId,
    Guid ConcurrencyToken);

public sealed record CopyPageResponse(
    IReadOnlyCollection<CopyResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
