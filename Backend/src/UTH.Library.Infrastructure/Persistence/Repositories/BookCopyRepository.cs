using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class BookCopyRepository(LibraryDbContext db) : IBookCopyRepository
{
    public async Task<PageResult<BookCopySnapshot>> GetPageAsync(BookCopyQuery query, CancellationToken cancellationToken)
    {
        var copies = db.BookCopies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            copies = copies.Where(copy => EF.Functions.ILike(copy.Barcode, search) ||
                db.Books.Any(book => book.Id == copy.BookId && EF.Functions.ILike(book.Title, search)));
        }
        if (query.BookId is Guid bookId) copies = copies.Where(copy => copy.BookId == bookId);
        if (query.ShelfId is Guid shelfId) copies = copies.Where(copy => copy.ShelfId == shelfId);
        if (query.BranchId is Guid branchId)
            copies = copies.Where(copy => copy.ShelfId != null &&
                db.Shelves.Any(shelf => shelf.Id == copy.ShelfId &&
                    db.Areas.Any(area => area.Id == shelf.AreaId && area.BranchId == branchId)));
        if (query.Condition is CopyCondition condition) copies = copies.Where(copy => copy.Condition == condition);
        if (query.Status is CopyStatus status) copies = copies.Where(copy => copy.Status == status);

        var total = await copies.CountAsync(cancellationToken);
        var items = await copies.OrderBy(copy => copy.Barcode).ThenBy(copy => copy.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(copy => new BookCopySnapshot(copy,
                db.Books.Where(book => book.Id == copy.BookId).Select(book => book.Title).First(),
                db.Shelves.Where(shelf => shelf.Id == copy.ShelfId).Select(shelf => shelf.Code).FirstOrDefault(),
                (from shelf in db.Shelves join area in db.Areas on shelf.AreaId equals area.Id
                 where shelf.Id == copy.ShelfId select (Guid?)area.BranchId).FirstOrDefault(),
                (from shelf in db.Shelves join area in db.Areas on shelf.AreaId equals area.Id
                 join branch in db.Branches on area.BranchId equals branch.Id
                 where shelf.Id == copy.ShelfId select branch.Code).FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new PageResult<BookCopySnapshot>(items, query.PageNumber, query.PageSize, total);
    }

    public Task<BookCopySnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        SnapshotQuery(db.BookCopies.AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BookCopySnapshot?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        SnapshotQuery(db.BookCopies.AsNoTracking().Where(x => x.Barcode.Trim().ToUpper() == barcode))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BookCopy?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.BookCopies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken) =>
        db.BookCopies.AnyAsync(x => x.Barcode.Trim().ToUpper() == barcode, cancellationToken);

    public Task<bool> ActiveBookExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Books.AnyAsync(x => x.Id == id && x.Status == RecordStatus.Active, cancellationToken);

    public Task<bool> ActiveShelfExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Shelves.AnyAsync(shelf => shelf.Id == id && shelf.Status == ShelfStatus.Active &&
            db.Areas.Any(area => area.Id == shelf.AreaId && area.IsActive &&
                db.Branches.Any(branch => branch.Id == area.BranchId && branch.IsActive)), cancellationToken);

    public Task<bool> ReceiptItemMatchesBookAsync(Guid id, Guid bookId, CancellationToken cancellationToken) =>
        db.StockReceiptItems.AnyAsync(x => x.Id == id && x.BookId == bookId &&
            db.StockReceipts.Any(receipt => receipt.Id == x.StockReceiptId &&
                receipt.Status == StockReceiptStatus.Confirmed), cancellationToken);

    public Task<bool> HasActiveAuditAsync(Guid id, CancellationToken cancellationToken) =>
        db.InventoryAuditItems.AnyAsync(item => item.BookCopyId == id &&
            db.InventoryAudits.Any(audit => audit.Id == item.InventoryAuditId &&
                (audit.Status == InventoryAuditStatus.Draft || audit.Status == InventoryAuditStatus.InProgress)), cancellationToken);

    public Task<bool> HasEditableReceiptAsync(Guid? stockReceiptItemId, CancellationToken cancellationToken) =>
        stockReceiptItemId is null
            ? Task.FromResult(false)
            : db.StockReceiptItems.AnyAsync(item => item.Id == stockReceiptItemId &&
                db.StockReceipts.Any(receipt => receipt.Id == item.StockReceiptId &&
                    (receipt.Status == StockReceiptStatus.Draft || receipt.Status == StockReceiptStatus.Received)), cancellationToken);

    public Task<bool> HasActiveBorrowingForBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        db.Borrowings.AnyAsync(x => x.BookId == bookId && x.ReturnedAtUtc == null, cancellationToken);

    public Task AddAsync(BookCopy copy, CancellationToken cancellationToken) =>
        db.BookCopies.AddAsync(copy, cancellationToken).AsTask();

    private IQueryable<BookCopySnapshot> SnapshotQuery(IQueryable<BookCopy> copies) =>
        from copy in copies
        join book in db.Books.AsNoTracking() on copy.BookId equals book.Id
        join shelf in db.Shelves.AsNoTracking() on copy.ShelfId equals (Guid?)shelf.Id into shelves
        from shelf in shelves.DefaultIfEmpty()
        join area in db.Areas.AsNoTracking() on shelf.AreaId equals area.Id into areas
        from area in areas.DefaultIfEmpty()
        join branch in db.Branches.AsNoTracking() on area.BranchId equals branch.Id into branches
        from branch in branches.DefaultIfEmpty()
        select new BookCopySnapshot(copy, book.Title, shelf == null ? null : shelf.Code,
            branch == null ? null : branch.Id, branch == null ? null : branch.Code);
}
