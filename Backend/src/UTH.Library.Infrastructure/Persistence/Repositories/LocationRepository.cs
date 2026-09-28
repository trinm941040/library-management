using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class LocationRepository(LibraryDbContext db) : ILocationRepository
{
    public async Task<IReadOnlyList<BranchLocationSnapshot>> GetHierarchyAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var branches = await db.Branches.AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var branchIds = branches.Select(x => x.Id).ToArray();
        var areas = await db.Areas.AsNoTracking()
            .Where(x => branchIds.Contains(x.BranchId) && (includeInactive || x.IsActive))
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var areaIds = areas.Select(x => x.Id).ToArray();
        var shelves = await db.Shelves.AsNoTracking()
            .Where(x => areaIds.Contains(x.AreaId) && (includeInactive || x.Status == ShelfStatus.Active))
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return branches.Select(branch =>
        {
            var branchAreas = areas.Where(x => x.BranchId == branch.Id)
                .Select(area => new AreaLocationSnapshot(area, shelves.Where(x => x.AreaId == area.Id).ToArray()))
                .ToArray();
            var activeAreas = branchAreas.Where(x => x.Area.IsActive).ToArray();
            var readiness = new BranchReadinessSnapshot(
                activeAreas.Length,
                activeAreas.Sum(x => x.Shelves.Count(shelf => shelf.Status == ShelfStatus.Active)));
            return new BranchLocationSnapshot(branch, branchAreas, readiness);
        }).ToArray();
    }

    public async Task<IReadOnlyList<ShelfLocationSnapshot>> GetActiveShelvesAsync(CancellationToken cancellationToken) =>
        await (from shelf in db.Shelves.AsNoTracking()
               join area in db.Areas.AsNoTracking() on shelf.AreaId equals area.Id
               join branch in db.Branches.AsNoTracking() on area.BranchId equals branch.Id
               where shelf.Status == ShelfStatus.Active && area.IsActive && branch.IsActive
               orderby branch.Code, area.Code, shelf.Code
               select new ShelfLocationSnapshot(
                   shelf.Id, shelf.Code, shelf.Label,
                   area.Id, area.Code, area.Name,
                   branch.Id, branch.Code, branch.Name))
            .ToListAsync(cancellationToken);

    public Task<Branch?> GetBranchAsync(Guid id, CancellationToken cancellationToken) =>
        db.Branches.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Area?> GetAreaAsync(Guid id, CancellationToken cancellationToken) =>
        db.Areas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Shelf?> GetShelfAsync(Guid id, CancellationToken cancellationToken) =>
        db.Shelves.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Shelf>> GetShelvesByAreaAsync(Guid areaId, CancellationToken cancellationToken) =>
        await db.Shelves.Where(x => x.AreaId == areaId).OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public Task<bool> BranchCodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Branches.AnyAsync(x => x.Code == code && (excludingId == null || x.Id != excludingId), cancellationToken);

    public Task<bool> AreaCodeExistsAsync(Guid branchId, string code, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Areas.AnyAsync(x => x.BranchId == branchId && x.Code == code && (excludingId == null || x.Id != excludingId), cancellationToken);

    public Task<bool> ShelfCodeExistsAsync(Guid areaId, string code, Guid? excludingId, CancellationToken cancellationToken) =>
        db.Shelves.AnyAsync(x => x.AreaId == areaId && x.Code == code && (excludingId == null || x.Id != excludingId), cancellationToken);

    public async Task<BranchReadinessSnapshot> GetBranchReadinessAsync(
        Guid branchId,
        Guid? excludingAreaId,
        Guid? excludingShelfId,
        CancellationToken cancellationToken)
    {
        var activeAreaIds = await db.Areas.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && (excludingAreaId == null || x.Id != excludingAreaId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var activeShelfCount = await db.Shelves.AsNoTracking()
            .CountAsync(x => activeAreaIds.Contains(x.AreaId) && x.Status == ShelfStatus.Active &&
                             (excludingShelfId == null || x.Id != excludingShelfId), cancellationToken);
        return new BranchReadinessSnapshot(activeAreaIds.Count, activeShelfCount);
    }

    public async Task<LocationImpactSnapshot> GetBranchImpactAsync(Guid branchId, CancellationToken cancellationToken)
    {
        var shelfIds = await ShelfIdsForBranch(branchId).ToListAsync(cancellationToken);
        var employeeCount = await db.Employees.AsNoTracking()
            .CountAsync(x => x.BranchId == branchId && x.Status != EmploymentStatus.Terminated, cancellationToken);
        var copyCount = await db.BookCopies.AsNoTracking()
            .CountAsync(x => x.ShelfId != null && shelfIds.Contains(x.ShelfId.Value) && x.Status != CopyStatus.Withdrawn, cancellationToken);
        var auditCount = await db.InventoryAudits.AsNoTracking()
            .CountAsync(x => x.BranchId == branchId &&
                             (x.Status == InventoryAuditStatus.Draft || x.Status == InventoryAuditStatus.InProgress), cancellationToken);
        var receiptCount = await db.StockReceipts.AsNoTracking()
            .CountAsync(x => x.BranchId == branchId &&
                             (x.Status == StockReceiptStatus.Draft || x.Status == StockReceiptStatus.Received), cancellationToken);
        return new LocationImpactSnapshot(employeeCount, copyCount, auditCount, receiptCount);
    }

    public async Task<LocationImpactSnapshot> GetAreaImpactAsync(Guid areaId, CancellationToken cancellationToken)
    {
        var shelfIds = db.Shelves.AsNoTracking().Where(x => x.AreaId == areaId).Select(x => x.Id);
        return await GetShelfSetImpactAsync(shelfIds, cancellationToken);
    }

    public Task<LocationImpactSnapshot> GetShelfImpactAsync(Guid shelfId, CancellationToken cancellationToken) =>
        GetShelfSetImpactAsync(db.Shelves.AsNoTracking().Where(x => x.Id == shelfId).Select(x => x.Id), cancellationToken);

    public Task<bool> BranchHasAreasAsync(Guid branchId, CancellationToken cancellationToken) =>
        db.Areas.AsNoTracking().AnyAsync(x => x.BranchId == branchId, cancellationToken);

    public async Task<bool> BranchHasReferencesAsync(Guid branchId, CancellationToken cancellationToken) =>
        await db.Employees.AsNoTracking().AnyAsync(x => x.BranchId == branchId, cancellationToken) ||
        await db.InventoryAudits.AsNoTracking().AnyAsync(x => x.BranchId == branchId, cancellationToken) ||
        await db.StockReceipts.AsNoTracking().AnyAsync(x => x.BranchId == branchId, cancellationToken) ||
        await db.CirculationPolicies.AsNoTracking().AnyAsync(x => x.BranchId == branchId, cancellationToken);

    public Task<bool> AreaHasShelvesAsync(Guid areaId, CancellationToken cancellationToken) =>
        db.Shelves.AsNoTracking().AnyAsync(x => x.AreaId == areaId, cancellationToken);

    public Task<bool> AreaHasReferencesAsync(Guid areaId, CancellationToken cancellationToken) =>
        db.InventoryAudits.AsNoTracking().AnyAsync(x => x.AreaId == areaId, cancellationToken);

    public Task<bool> ShelfHasBookCopiesAsync(Guid shelfId, CancellationToken cancellationToken) =>
        db.BookCopies.AsNoTracking().AnyAsync(x => x.ShelfId == shelfId, cancellationToken);

    public Task<bool> ShelfHasReferencesAsync(Guid shelfId, CancellationToken cancellationToken) =>
        db.InventoryAudits.AsNoTracking().AnyAsync(x => x.ShelfId == shelfId, cancellationToken);

    public Task AddBranchAsync(Branch branch, CancellationToken cancellationToken) =>
        db.Branches.AddAsync(branch, cancellationToken).AsTask();

    public Task AddAreaAsync(Area area, CancellationToken cancellationToken) =>
        db.Areas.AddAsync(area, cancellationToken).AsTask();

    public Task AddShelfAsync(Shelf shelf, CancellationToken cancellationToken) =>
        db.Shelves.AddAsync(shelf, cancellationToken).AsTask();

    public void RemoveBranch(Branch branch) => db.Branches.Remove(branch);
    public void RemoveArea(Area area) => db.Areas.Remove(area);
    public void RemoveShelf(Shelf shelf) => db.Shelves.Remove(shelf);

    private IQueryable<Guid> ShelfIdsForBranch(Guid branchId) =>
        from shelf in db.Shelves.AsNoTracking()
        join area in db.Areas.AsNoTracking() on shelf.AreaId equals area.Id
        where area.BranchId == branchId
        select shelf.Id;

    private async Task<LocationImpactSnapshot> GetShelfSetImpactAsync(IQueryable<Guid> shelfIdsQuery, CancellationToken cancellationToken)
    {
        var shelfIds = await shelfIdsQuery.ToListAsync(cancellationToken);
        var copyCount = await db.BookCopies.AsNoTracking()
            .CountAsync(x => x.ShelfId != null && shelfIds.Contains(x.ShelfId.Value) && x.Status != CopyStatus.Withdrawn, cancellationToken);
        var activeAuditIds = db.InventoryAudits.AsNoTracking()
            .Where(x => x.Status == InventoryAuditStatus.Draft || x.Status == InventoryAuditStatus.InProgress)
            .Select(x => x.Id);
        var auditCount = await db.InventoryAuditItems.AsNoTracking()
            .Where(x => activeAuditIds.Contains(x.InventoryAuditId) &&
                        ((x.ExpectedShelfId != null && shelfIds.Contains(x.ExpectedShelfId.Value)) ||
                         (x.ActualShelfId != null && shelfIds.Contains(x.ActualShelfId.Value))))
            .Select(x => x.InventoryAuditId)
            .Distinct()
            .CountAsync(cancellationToken);
        return new LocationImpactSnapshot(0, copyCount, auditCount, 0);
    }
}
