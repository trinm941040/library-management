using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.StockReceipts;

public sealed record StockReceiptItemModel(Guid Id, Guid BookId, string BookTitle, string Isbn,
    int ExpectedQuantity, int ReceivedQuantity, int DamagedQuantity, decimal? UnitCost,
    decimal TotalValue, Guid ConcurrencyToken);

public sealed record StockReceiptModel(Guid Id, string ReceiptNumber, Guid SupplierId, string SupplierName,
    Guid BranchId, string BranchCode, Guid ReceivedByUserId, StockReceiptStatus Status,
    DateTime ReceivedAtUtc, string? Notes, Guid ConcurrencyToken, long TotalQuantity,
    decimal TotalValue, IReadOnlyList<StockReceiptItemModel> Items);

public sealed record SaveStockReceiptItemCommand(Guid? Id, Guid BookId, int ExpectedQuantity,
    int ReceivedQuantity, int DamagedQuantity, decimal? UnitCost);

public sealed record SaveStockReceiptCommand(Guid SupplierId, Guid BranchId, DateTime ReceivedAtUtc,
    string? Notes, IReadOnlyList<SaveStockReceiptItemCommand> Items, Guid? ConcurrencyToken = null);
