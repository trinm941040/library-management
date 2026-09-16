using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class SavedFilterRepository(LibraryDbContext dbContext) : ISavedFilterRepository
{
    public async Task<IReadOnlyList<SavedFilter>> GetByUserAndScopeAsync(
        Guid userId,
        string scope,
        CancellationToken cancellationToken)
    {
        var normalizedScope = scope.Trim().ToLowerInvariant();
        return await dbContext.SavedFilters
            .AsNoTracking()
            .Where(f => f.OwnerUserId == userId && f.Scope == normalizedScope)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<SavedFilter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.SavedFilters.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<SavedFilter> CreateAsync(SavedFilter filter, CancellationToken cancellationToken)
    {
        dbContext.SavedFilters.Add(filter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return filter;
    }

    public async Task UpdateAsync(SavedFilter filter, CancellationToken cancellationToken)
    {
        dbContext.SavedFilters.Update(filter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SavedFilter filter, CancellationToken cancellationToken)
    {
        dbContext.SavedFilters.Remove(filter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
