using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface ILocationRepository
{
    Task<IReadOnlyList<BranchLocationSnapshot>> GetHierarchyAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShelfLocationSnapshot>> GetActiveShelvesAsync(CancellationToken cancellationToken);
    Task<Branch?> GetBranchAsync(Guid id, CancellationToken cancellationToken);
    Task<Area?> GetAreaAsync(Guid id, CancellationToken cancellationToken);
    Task<Shelf?> GetShelfAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Shelf>> GetShelvesByAreaAsync(Guid areaId, CancellationToken cancellationToken);
    Task<bool> BranchCodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> AreaCodeExistsAsync(Guid branchId, string code, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> ShelfCodeExistsAsync(Guid areaId, string code, Guid? excludingId, CancellationToken cancellationToken);
    Task<BranchReadinessSnapshot> GetBranchReadinessAsync(Guid branchId, Guid? excludingAreaId, Guid? excludingShelfId, CancellationToken cancellationToken);
    Task<LocationImpactSnapshot> GetBranchImpactAsync(Guid branchId, CancellationToken cancellationToken);
    Task<LocationImpactSnapshot> GetAreaImpactAsync(Guid areaId, CancellationToken cancellationToken);
    Task<LocationImpactSnapshot> GetShelfImpactAsync(Guid shelfId, CancellationToken cancellationToken);
    Task AddBranchAsync(Branch branch, CancellationToken cancellationToken);
    Task AddAreaAsync(Area area, CancellationToken cancellationToken);
    Task AddShelfAsync(Shelf shelf, CancellationToken cancellationToken);
}

public sealed record BranchLocationSnapshot(
    Branch Branch,
    IReadOnlyList<AreaLocationSnapshot> Areas,
    BranchReadinessSnapshot Readiness);

public sealed record AreaLocationSnapshot(Area Area, IReadOnlyList<Shelf> Shelves);

public sealed record ShelfLocationSnapshot(
    Guid Id,
    string Code,
    string Label,
    Guid AreaId,
    string AreaCode,
    string AreaName,
    Guid BranchId,
    string BranchCode,
    string BranchName);

public sealed record BranchReadinessSnapshot(
    int ActiveAreaCount,
    int ActiveShelfCount)
{
    public bool CanActivate => ActiveAreaCount > 0 && ActiveShelfCount > 0;
}

public sealed record LocationImpactSnapshot(
    int EmployeeCount,
    int BookCopyCount,
    int ActiveInventoryAuditCount,
    int EditableStockReceiptCount)
{
    public bool HasBlockingReferences =>
        EmployeeCount > 0 || BookCopyCount > 0 || ActiveInventoryAuditCount > 0 || EditableStockReceiptCount > 0;
}
