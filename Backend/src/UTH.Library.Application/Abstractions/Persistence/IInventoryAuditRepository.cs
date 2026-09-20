using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record InventoryAuditQuery(Guid? BranchId, InventoryAuditStatus? Status,
    int PageNumber, int PageSize);
public sealed record InventoryAuditItemSnapshot(InventoryAuditItem Item, string Barcode,
    string BookTitle, Guid CopyConcurrencyToken);
public sealed record InventoryAuditSnapshot(InventoryAudit Audit,
    IReadOnlyList<InventoryAuditItemSnapshot> Items);

public interface IInventoryAuditRepository
{
    Task<PageResult<InventoryAudit>> GetPageAsync(InventoryAuditQuery query, CancellationToken cancellationToken);
    Task<InventoryAuditSnapshot?> GetSnapshotAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryAudit?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryAuditItem>> GetTrackedItemsAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryAuditItem?> GetTrackedItemAsync(Guid auditId, Guid copyId, CancellationToken cancellationToken);
    Task<BookCopy?> GetCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<BookCopy?> GetTrackedCopyAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ScopeActiveAsync(Guid branchId, Guid? areaId, Guid? shelfId, CancellationToken cancellationToken);
    Task<bool> ShelfExistsAsync(Guid shelfId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookCopy>> GetExpectedCopiesAsync(Guid branchId, Guid? areaId,
        Guid? shelfId, CancellationToken cancellationToken);
    Task AddAsync(InventoryAudit audit, CancellationToken cancellationToken);
    Task AddItemsAsync(IReadOnlyCollection<InventoryAuditItem> items, CancellationToken cancellationToken);
    Task AddItemAsync(InventoryAuditItem item, CancellationToken cancellationToken);
}
