using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class StockReceiptRepository(LibraryDbContext db) : IStockReceiptRepository
{
    public async Task<PageResult<StockReceiptSnapshot>> GetPageAsync(StockReceiptQuery query, CancellationToken cancellationToken)
    {
        var receipts = db.StockReceipts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            receipts = receipts.Where(x => EF.Functions.ILike(x.ReceiptNumber, pattern) ||
                db.Suppliers.Any(supplier => supplier.Id == x.SupplierId && EF.Functions.ILike(supplier.Name, pattern)));
        }
        if (query.FromUtc is DateTime from) receipts = receipts.Where(x => x.ReceivedAtUtc >= from);
        if (query.ToUtc is DateTime to) receipts = receipts.Where(x => x.ReceivedAtUtc <= to);
        if (query.SupplierId is Guid supplierId) receipts = receipts.Where(x => x.SupplierId == supplierId);
        if (query.BranchId is Guid branchId) receipts = receipts.Where(x => x.BranchId == branchId);
        if (query.Status is StockReceiptStatus status) receipts = receipts.Where(x => x.Status == status);
        var count = await receipts.CountAsync(cancellationToken);
        var ids = await receipts.OrderByDescending(x => x.ReceivedAtUtc).ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => x.Id).ToArrayAsync(cancellationToken);
        var snapshots = await LoadAsync(ids, cancellationToken);
        var ordered = ids.Select(id => snapshots[id]).ToArray();
        return new PageResult<StockReceiptSnapshot>(ordered, query.PageNumber, query.PageSize, count);
    }

    public async Task<StockReceiptSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await LoadAsync([id], cancellationToken)).GetValueOrDefault(id);

    public Task<StockReceipt?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.StockReceipts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StockReceiptItem>> GetTrackedItemsAsync(Guid receiptId, CancellationToken cancellationToken) =>
        await db.StockReceiptItems.Where(x => x.StockReceiptId == receiptId).OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<bool> SupplierActiveAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers.AnyAsync(x => x.Id == id && x.Status == RecordStatus.Active, cancellationToken);

    public Task<bool> BranchActiveAsync(Guid id, CancellationToken cancellationToken) =>
        db.Branches.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetActiveBookIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        (await db.Books.AsNoTracking().Where(x => ids.Contains(x.Id) && x.Status == RecordStatus.Active)
            .Select(x => x.Id).ToArrayAsync(cancellationToken)).ToHashSet();

    public Task<bool> HasCopyReferencesAsync(Guid itemId, CancellationToken cancellationToken) =>
        db.BookCopies.AnyAsync(x => x.StockReceiptItemId == itemId, cancellationToken);

    public Task<bool> ReceiptNumberExistsAsync(string number, CancellationToken cancellationToken) =>
        db.StockReceipts.AnyAsync(x => x.ReceiptNumber == number, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetActiveShelfIdsAsync(Guid branchId,
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        (await (from shelf in db.Shelves.AsNoTracking()
            join area in db.Areas.AsNoTracking() on shelf.AreaId equals area.Id
            join branch in db.Branches.AsNoTracking() on area.BranchId equals branch.Id
            where ids.Contains(shelf.Id) && shelf.Status == ShelfStatus.Active &&
                area.IsActive && branch.IsActive && branch.Id == branchId
            select shelf.Id).ToArrayAsync(cancellationToken)).ToHashSet();

    public Task<bool> AnyBarcodeExistsAsync(IReadOnlyCollection<string> barcodes, CancellationToken cancellationToken) =>
        db.BookCopies.AnyAsync(copy => barcodes.Contains(copy.Barcode.Trim().ToUpper()), cancellationToken);

    public async Task<IReadOnlyList<BookCopy>> GetReceiptCopiesAsync(Guid receiptId, CancellationToken cancellationToken) =>
        await db.BookCopies.AsNoTracking().Where(copy => copy.StockReceiptItemId != null &&
            db.StockReceiptItems.Any(item => item.Id == copy.StockReceiptItemId && item.StockReceiptId == receiptId))
            .OrderBy(copy => copy.Barcode).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<DiscrepancyReport>> GetDiscrepanciesAsync(Guid receiptId, CancellationToken cancellationToken) =>
        await db.DiscrepancyReports.AsNoTracking().Where(report => report.StockReceiptId == receiptId)
            .OrderBy(report => report.CreatedAtUtc).ToArrayAsync(cancellationToken);

    public Task AddAsync(StockReceipt receipt, CancellationToken cancellationToken) =>
        db.StockReceipts.AddAsync(receipt, cancellationToken).AsTask();

    public Task AddItemAsync(StockReceiptItem item, CancellationToken cancellationToken) =>
        db.StockReceiptItems.AddAsync(item, cancellationToken).AsTask();

    public Task AddCopyAsync(BookCopy copy, CancellationToken cancellationToken) =>
        db.BookCopies.AddAsync(copy, cancellationToken).AsTask();

    public Task AddDiscrepancyAsync(DiscrepancyReport report, CancellationToken cancellationToken) =>
        db.DiscrepancyReports.AddAsync(report, cancellationToken).AsTask();

    public void RemoveItem(StockReceiptItem item) => db.StockReceiptItems.Remove(item);

    private async Task<Dictionary<Guid, StockReceiptSnapshot>> LoadAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        var headers = await (from receipt in db.StockReceipts.AsNoTracking()
            join supplier in db.Suppliers.AsNoTracking() on receipt.SupplierId equals supplier.Id
            join branch in db.Branches.AsNoTracking() on receipt.BranchId equals branch.Id
            where ids.Contains(receipt.Id)
            select new { Receipt = receipt, SupplierName = supplier.Name, BranchCode = branch.Code })
            .ToArrayAsync(cancellationToken);
        var items = await (from item in db.StockReceiptItems.AsNoTracking()
            join book in db.Books.AsNoTracking() on item.BookId equals book.Id
            where ids.Contains(item.StockReceiptId)
            orderby item.Id
            select new { Item = item, BookTitle = book.Title, book.Isbn })
            .ToArrayAsync(cancellationToken);
        return headers.ToDictionary(x => x.Receipt.Id, x => new StockReceiptSnapshot(x.Receipt,
            x.SupplierName, x.BranchCode, items.Where(item => item.Item.StockReceiptId == x.Receipt.Id)
                .Select(item => new StockReceiptItemSnapshot(item.Item, item.BookTitle, item.Isbn)).ToArray()));
    }
}
