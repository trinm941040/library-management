using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class InventoryAuditRepository(LibraryDbContext db) : IInventoryAuditRepository
{
    public async Task<PageResult<InventoryAudit>> GetPageAsync(InventoryAuditQuery query,
        CancellationToken cancellationToken)
    {
        var audits = db.InventoryAudits.AsNoTracking().AsQueryable();
        if (query.BranchId is Guid branchId) audits = audits.Where(audit => audit.BranchId == branchId);
        if (query.Status is InventoryAuditStatus status) audits = audits.Where(audit => audit.Status == status);
        var count = await audits.CountAsync(cancellationToken);
        var items = await audits.OrderByDescending(audit => audit.StartedAtUtc).ThenByDescending(audit => audit.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageResult<InventoryAudit>(items, query.PageNumber, query.PageSize, count);
    }

    public async Task<InventoryAuditSnapshot?> GetSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        var audit = await db.InventoryAudits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (audit is null) return null;
        var items = await (from item in db.InventoryAuditItems.AsNoTracking()
            join copy in db.BookCopies.AsNoTracking() on item.BookCopyId equals copy.Id
            join book in db.Books.AsNoTracking() on copy.BookId equals book.Id
            where item.InventoryAuditId == id
            orderby copy.Barcode
            select new InventoryAuditItemSnapshot(item, copy.Barcode, book.Title, copy.ConcurrencyToken))
            .ToArrayAsync(cancellationToken);
        return new InventoryAuditSnapshot(audit, items);
    }

    public Task<InventoryAudit?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.InventoryAudits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InventoryAuditItem>> GetTrackedItemsAsync(Guid id, CancellationToken cancellationToken) =>
        await db.InventoryAuditItems.Where(item => item.InventoryAuditId == id).ToArrayAsync(cancellationToken);

    public Task<InventoryAuditItem?> GetTrackedItemAsync(Guid auditId, Guid copyId, CancellationToken cancellationToken) =>
        db.InventoryAuditItems.SingleOrDefaultAsync(item => item.InventoryAuditId == auditId &&
            item.BookCopyId == copyId, cancellationToken);

    public Task<BookCopy?> GetCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        db.BookCopies.AsNoTracking().SingleOrDefaultAsync(copy => copy.Barcode.Trim().ToUpper() == barcode,
            cancellationToken);

    public Task<BookCopy?> GetTrackedCopyAsync(Guid id, CancellationToken cancellationToken) =>
        db.BookCopies.SingleOrDefaultAsync(copy => copy.Id == id, cancellationToken);

    public async Task<bool> ScopeActiveAsync(Guid branchId, Guid? areaId, Guid? shelfId,
        CancellationToken cancellationToken)
    {
        if (!await db.Branches.AnyAsync(branch => branch.Id == branchId && branch.IsActive, cancellationToken)) return false;
        if (areaId is Guid area && !await db.Areas.AnyAsync(value => value.Id == area &&
            value.BranchId == branchId && value.IsActive, cancellationToken)) return false;
        if (shelfId is Guid shelf && !await (from value in db.Shelves
            join parent in db.Areas on value.AreaId equals parent.Id
            where value.Id == shelf && value.Status == ShelfStatus.Active &&
                parent.Id == areaId && parent.BranchId == branchId && parent.IsActive
            select value.Id).AnyAsync(cancellationToken)) return false;
        return true;
    }

    public Task<bool> ShelfExistsAsync(Guid shelfId, CancellationToken cancellationToken) =>
        db.Shelves.AnyAsync(shelf => shelf.Id == shelfId, cancellationToken);

    public async Task<IReadOnlyList<BookCopy>> GetExpectedCopiesAsync(Guid branchId, Guid? areaId,
        Guid? shelfId, CancellationToken cancellationToken) =>
        await (from copy in db.BookCopies.AsNoTracking()
            join shelf in db.Shelves.AsNoTracking() on copy.ShelfId equals shelf.Id
            join area in db.Areas.AsNoTracking() on shelf.AreaId equals area.Id
            where area.BranchId == branchId && (areaId == null || area.Id == areaId) &&
                (shelfId == null || shelf.Id == shelfId) &&
                (copy.Status == CopyStatus.Available || copy.Status == CopyStatus.Reserved ||
                 copy.Status == CopyStatus.Damaged)
            select copy).ToArrayAsync(cancellationToken);

    public Task AddAsync(InventoryAudit audit, CancellationToken cancellationToken) =>
        db.InventoryAudits.AddAsync(audit, cancellationToken).AsTask();

    public Task AddItemsAsync(IReadOnlyCollection<InventoryAuditItem> items, CancellationToken cancellationToken) =>
        db.InventoryAuditItems.AddRangeAsync(items, cancellationToken);

    public Task AddItemAsync(InventoryAuditItem item, CancellationToken cancellationToken) =>
        db.InventoryAuditItems.AddAsync(item, cancellationToken).AsTask();
}
