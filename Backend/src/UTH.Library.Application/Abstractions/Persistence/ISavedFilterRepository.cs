using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface ISavedFilterRepository
{
    Task<IReadOnlyList<SavedFilter>> GetByUserAndScopeAsync(Guid userId, string scope, CancellationToken cancellationToken);
    Task<SavedFilter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SavedFilter> CreateAsync(SavedFilter filter, CancellationToken cancellationToken);
    Task UpdateAsync(SavedFilter filter, CancellationToken cancellationToken);
    Task DeleteAsync(SavedFilter filter, CancellationToken cancellationToken);
}
