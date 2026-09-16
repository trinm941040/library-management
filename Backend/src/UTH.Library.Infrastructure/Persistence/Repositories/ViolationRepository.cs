using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class ViolationRepository(LibraryDbContext dbContext) : IViolationRepository
{
    public Task AddAsync(Violation violation, CancellationToken cancellationToken) =>
        dbContext.Violations.AddAsync(violation, cancellationToken).AsTask();

    public Task<Violation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Violations.SingleOrDefaultAsync(violation => violation.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Violation> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Violations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(violation =>
                EF.Functions.ILike(violation.BorrowerName, $"%{keyword}%") ||
                EF.Functions.ILike(violation.BorrowerEmail, $"%{keyword}%") ||
                EF.Functions.ILike(violation.BookTitle, $"%{keyword}%"));
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "open" => query.Where(violation => violation.ResolvedAtUtc == null),
            "paid" => query.Where(violation => violation.Resolution == "paid"),
            "waived" => query.Where(violation => violation.Resolution == "waived"),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(violation => violation.RecordedAtUtc)
            .ThenByDescending(violation => violation.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
