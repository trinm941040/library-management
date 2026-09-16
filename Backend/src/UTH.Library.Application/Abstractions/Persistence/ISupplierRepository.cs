using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface ISupplierRepository
{
    Task<PageResult<SupplierSnapshot>> GetPageAsync(string? search, RecordStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupplierSnapshot>> GetActiveAsync(CancellationToken cancellationToken);
    Task<SupplierSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Supplier?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> HasReceiptsAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken);
}

public sealed record SupplierSnapshot(Supplier Supplier, bool HasStockReceipts);
