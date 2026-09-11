namespace UTH.Library.Application.Features.CirculationPolicies;

public interface ICirculationPolicyResolver
{
    Task<ResolvedCirculationPolicy> ResolveAsync(
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime? effectiveAtUtc,
        CancellationToken cancellationToken);

    decimal CalculateFine(ResolvedCirculationPolicy policy, int overdueDays);

    decimal CalculateLostPenalty(ResolvedCirculationPolicy policy, decimal bookPrice);

    void InvalidateCache();
}
