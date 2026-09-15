using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface ICirculationPolicyRepository
{
    Task<(IReadOnlyList<CirculationPolicy> Items, int TotalCount)> GetPageAsync(
        string? search,
        bool? isActive,
        string? memberGroup,
        Guid? branchId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CirculationPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CirculationPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken);

    Task<bool> HasOverlappingActivePolicyAsync(
        Guid? excludeId,
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        CancellationToken cancellationToken);

    Task AddAsync(CirculationPolicy policy, CancellationToken cancellationToken);

    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

