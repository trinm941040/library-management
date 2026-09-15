namespace UTH.Library.Application.Abstractions.Identity;

public sealed record BorrowerRecord(Guid Id, string DisplayName, string Email, bool IsActive);

public interface IBorrowerLookup
{
    Task<BorrowerRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BorrowerRecord>> ListActiveAsync(int take, CancellationToken cancellationToken);
}
