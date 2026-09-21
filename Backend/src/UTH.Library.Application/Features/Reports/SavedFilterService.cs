using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Reports;

public sealed class SavedFilterService(
    ISavedFilterRepository repository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<SavedFilterDto>> GetUserFiltersAsync(
        Guid userId,
        string scope,
        CancellationToken cancellationToken)
    {
        var list = await repository.GetByUserAndScopeAsync(userId, scope, cancellationToken);
        return list.Select(x => new SavedFilterDto(
            x.Id,
            x.Name,
            x.Scope,
            x.Criteria,
            x.Sort,
            x.CreatedAtUtc
        )).ToList();
    }

    public async Task<SavedFilterDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var filter = await repository.GetByIdAsync(id, cancellationToken);
        if (filter is null || filter.OwnerUserId != userId)
        {
            return null;
        }

        return new SavedFilterDto(
            filter.Id,
            filter.Name,
            filter.Scope,
            filter.Criteria,
            filter.Sort,
            filter.CreatedAtUtc);
    }

    public async Task<SavedFilterDto> CreateAsync(
        Guid userId,
        CreateSavedFilterCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var entity = SavedFilter.Create(
            userId,
            command.Name,
            command.Scope,
            command.Criteria,
            command.Sort,
            nowUtc);

        var created = await repository.CreateAsync(entity, cancellationToken);
        return new SavedFilterDto(
            created.Id,
            created.Name,
            created.Scope,
            created.Criteria,
            created.Sort,
            created.CreatedAtUtc);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Guid userId,
        UpdateSavedFilterCommand command,
        CancellationToken cancellationToken)
    {
        var filter = await repository.GetByIdAsync(id, cancellationToken);
        if (filter is null || filter.OwnerUserId != userId)
        {
            return false;
        }

        filter.Update(command.Name, command.Criteria, command.Sort);
        await repository.UpdateAsync(filter, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var filter = await repository.GetByIdAsync(id, cancellationToken);
        if (filter is null || filter.OwnerUserId != userId)
        {
            return false;
        }

        await repository.DeleteAsync(filter, cancellationToken);
        return true;
    }
}
