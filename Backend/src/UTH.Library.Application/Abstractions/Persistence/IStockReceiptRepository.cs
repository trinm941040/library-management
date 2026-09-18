using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record StockReceiptQuery(string? Search, DateTime? FromUtc, DateTime? ToUtc,
    Guid? SupplierId, Guid? BranchId, StockReceiptStatus? Status, int PageNumber, int PageSize);

public sealed record StockReceiptItemSnapshot(StockReceiptItem Item, string BookTitle, string Isbn);
public sealed record StockReceiptSnapshot(StockReceipt Receipt, string SupplierName, string BranchCode,
    IReadOnlyList<StockReceiptItemSnapshot> Items);

public interface IStockReceiptRepository
{
    Task<PageResult<StockReceiptSnapshot>> GetPageAsync(StockReceiptQuery query, CancellationToken cancellationToken);
    Task<StockReceiptSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StockReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockReceiptItem>> GetTrackedItemsAsync(Guid receiptId, CancellationToken cancellationToken);
    Task<bool> SupplierActiveAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> BranchActiveAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetActiveBookIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> HasCopyReferencesAsync(Guid itemId, CancellationToken cancellationToken);
    Task<bool> ReceiptNumberExistsAsync(string number, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetActiveShelfIdsAsync(Guid branchId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> AnyBarcodeExistsAsync(IReadOnlyCollection<string> barcodes, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookCopy>> GetReceiptCopiesAsync(Guid receiptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DiscrepancyReport>> GetDiscrepanciesAsync(Guid receiptId, CancellationToken cancellationToken);
    Task AddAsync(StockReceipt receipt, CancellationToken cancellationToken);
    Task AddItemAsync(StockReceiptItem item, CancellationToken cancellationToken);
    Task AddCopyAsync(BookCopy copy, CancellationToken cancellationToken);
    Task AddDiscrepancyAsync(DiscrepancyReport report, CancellationToken cancellationToken);
    void RemoveItem(StockReceiptItem item);
}
