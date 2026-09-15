using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class CirculationPolicyRepository(LibraryDbContext db) : ICirculationPolicyRepository
{
    public async Task<(IReadOnlyList<CirculationPolicy> Items, int TotalCount)> GetPageAsync(
        string? search,
        bool? isActive,
        string? memberGroup,
        Guid? branchId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.CirculationPolicies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(memberGroup))
        {
            var mg = memberGroup.Trim().ToLower();
            query = query.Where(p => p.MemberGroup != null && p.MemberGroup.ToLower() == mg);
        }

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(p => p.BranchId == branchId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ThenByDescending(p => p.Version)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<CirculationPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.CirculationPolicies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CirculationPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken) =>
        await db.CirculationPolicies
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOverlappingActivePolicyAsync(
        Guid? excludeId,
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        CancellationToken cancellationToken)
    {
        var normMember = NormalizeScope(memberGroup);
        var normDoc = NormalizeScope(documentType);
        var end = effectiveTo ?? DateTime.MaxValue;

        var activePolicies = await db.CirculationPolicies
            .Where(p => p.IsActive && (!excludeId.HasValue || p.Id != excludeId.Value))
            .ToListAsync(cancellationToken);

        return activePolicies.Any(p =>
        {
            bool sameMember = string.Equals(p.MemberGroup, normMember, StringComparison.OrdinalIgnoreCase);
            bool sameDoc = string.Equals(p.DocumentType, normDoc, StringComparison.OrdinalIgnoreCase);
            bool sameBranch = p.BranchId == branchId;

            if (!sameMember || !sameDoc || !sameBranch)
                return false;

            var pEnd = p.EffectiveTo ?? DateTime.MaxValue;
            return p.EffectiveFrom < end && effectiveFrom < pEnd;
        });
    }

    public Task AddAsync(CirculationPolicy policy, CancellationToken cancellationToken) =>
        db.CirculationPolicies.AddAsync(policy, cancellationToken).AsTask();

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        db.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    private static string? NormalizeScope(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
}
