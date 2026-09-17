using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.StockReceipts;

public sealed class StockReceiptFilterRequest
{
    public string? Search { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? BranchId { get; init; }
    public StockReceiptStatus? Status { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record StockReceiptItemRequest(Guid? Id, Guid BookId,
    [Range(0, 1_000_000)] int ExpectedQuantity,
    [Range(0, 1_000_000)] int ReceivedQuantity,
    [Range(0, 1_000_000)] int DamagedQuantity,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal? UnitCost);

public sealed record SaveStockReceiptRequest(Guid SupplierId, Guid BranchId, DateTime ReceivedAtUtc,
    [StringLength(2000)] string? Notes, [Required] IReadOnlyList<StockReceiptItemRequest> Items,
    Guid? ConcurrencyToken = null);

public sealed record StockReceiptItemResponse(Guid Id, Guid BookId, string BookTitle, string Isbn,
    int ExpectedQuantity, int ReceivedQuantity, int DamagedQuantity, decimal? UnitCost,
    decimal TotalValue, Guid ConcurrencyToken);

public sealed record StockReceiptResponse(Guid Id, string ReceiptNumber, Guid SupplierId, string SupplierName,
    Guid BranchId, string BranchCode, Guid ReceivedByUserId, StockReceiptStatus Status,
    DateTime ReceivedAtUtc, string? Notes, Guid ConcurrencyToken, long TotalQuantity,
    decimal TotalValue, IReadOnlyList<StockReceiptItemResponse> Items);

public sealed record StockReceiptPageResponse(IReadOnlyList<StockReceiptResponse> Items,
    int PageNumber, int PageSize, int TotalCount, int TotalPages);

public sealed record ConfirmReceiptCopyRequest([Required] string Barcode, Guid ShelfId, CopyCondition Condition);
public sealed record ConfirmReceiptItemRequest(Guid StockReceiptItemId,
    [Required] IReadOnlyList<ConfirmReceiptCopyRequest> Copies);
public sealed record ConfirmStockReceiptRequest(Guid ConcurrencyToken,
    [Required] IReadOnlyList<ConfirmReceiptItemRequest> Items);
public sealed record ReceiptCopyResponse(Guid Id, Guid StockReceiptItemId, string Barcode,
    Guid ShelfId, CopyCondition Condition, CopyStatus Status);
public sealed record DiscrepancyResponse(Guid Id, DiscrepancyType Type, int ExpectedQuantity,
    int ActualQuantity, string Description, DateTime CreatedAtUtc, Guid? CreatedByUserId);
public sealed record ConfirmStockReceiptResponse(StockReceiptResponse Receipt,
    IReadOnlyList<ReceiptCopyResponse> Copies, IReadOnlyList<DiscrepancyResponse> Discrepancies);
