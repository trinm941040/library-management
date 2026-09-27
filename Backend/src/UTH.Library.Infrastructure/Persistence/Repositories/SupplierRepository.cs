using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class SupplierRepository(LibraryDbContext db) : ISupplierRepository
{
    public async Task<PageResult<SupplierSnapshot>> GetPageAsync(string? search, RecordStatus? status,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, pattern) ||
                EF.Functions.ILike(x.Name, pattern) ||
                (x.ContactName != null && EF.Functions.ILike(x.ContactName, pattern)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                (x.PhoneNumber != null && EF.Functions.ILike(x.PhoneNumber, pattern)));
        }
        if (status is not null) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new SupplierSnapshot(x, db.StockReceipts.Any(receipt => receipt.SupplierId == x.Id)))
            .ToListAsync(cancellationToken);
        return new PageResult<SupplierSnapshot>(items, pageNumber, pageSize, total);
    }

    public async Task<IReadOnlyList<SupplierSnapshot>> GetActiveAsync(CancellationToken cancellationToken) =>
        await db.Suppliers.AsNoTracking().Where(x => x.Status == RecordStatus.Active)
            .OrderBy(x => x.Name).ThenBy(x => x.Code)
            .Select(x => new SupplierSnapshot(x, db.StockReceipts.Any(receipt => receipt.SupplierId == x.Id)))
            .ToListAsync(cancellationToken);

    public Task<SupplierSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new SupplierSnapshot(x, db.StockReceipts.Any(receipt => receipt.SupplierId == x.Id)))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Supplier?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken) =>
        db.Suppliers.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId), cancellationToken);

    public Task<bool> HasReceiptsAsync(Guid id, CancellationToken cancellationToken) =>
        db.StockReceipts.AnyAsync(x => x.SupplierId == id, cancellationToken);

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken) =>
        db.Suppliers.AddAsync(supplier, cancellationToken).AsTask();

    private static string EscapeLike(string text) => text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
