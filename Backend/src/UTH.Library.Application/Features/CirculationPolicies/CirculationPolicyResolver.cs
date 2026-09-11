using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.CirculationPolicies;

public sealed class CirculationPolicyResolver(
    ICirculationPolicyRepository repository,
    TimeProvider timeProvider) : ICirculationPolicyResolver
{
    private static readonly object CacheLock = new();
    private static IReadOnlyList<CirculationPolicy>? _cachedPolicies;
    private static DateTime _cacheExpiresAtUtc = DateTime.MinValue;

    public async Task<ResolvedCirculationPolicy> ResolveAsync(
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime? effectiveAtUtc,
        CancellationToken cancellationToken)
    {
        var now = effectiveAtUtc ?? timeProvider.GetUtcNow().UtcDateTime;
        var policies = await GetCachedActivePoliciesAsync(cancellationToken);

        // Lọc các chính sách có hiệu lực tại thời điểm 'now'
        var validPolicies = policies.Where(p =>
            p.EffectiveFrom <= now && (p.EffectiveTo is null || p.EffectiveTo.Value >= now));

        ResolvedCirculationPolicy? bestMatch = null;
        var highestScore = -1;
        var bestVersion = -1;
        var bestEffectiveFrom = DateTime.MinValue;

        foreach (var policy in validPolicies)
        {
            // Kiểm tra Branch match
            bool branchMatches = policy.BranchId is null || (branchId.HasValue && policy.BranchId == branchId.Value);
            if (!branchMatches) continue;

            // Kiểm tra MemberGroup match
            bool memberMatches = string.IsNullOrWhiteSpace(policy.MemberGroup) ||
                (memberGroup is not null && string.Equals(policy.MemberGroup, memberGroup, StringComparison.OrdinalIgnoreCase));
            if (!memberMatches) continue;

            // Kiểm tra DocumentType match
            bool docMatches = string.IsNullOrWhiteSpace(policy.DocumentType) ||
                (documentType is not null && string.Equals(policy.DocumentType, documentType, StringComparison.OrdinalIgnoreCase));
            if (!docMatches) continue;

            // Tính Specificity Score:
            // Branch cụ thể (+4), MemberGroup cụ thể (+2), DocumentType cụ thể (+1)
            var score = 0;
            if (policy.BranchId.HasValue) score += 4;
            if (!string.IsNullOrWhiteSpace(policy.MemberGroup)) score += 2;
            if (!string.IsNullOrWhiteSpace(policy.DocumentType)) score += 1;

            bool isBetter = score > highestScore ||
                (score == highestScore && (policy.Version > bestVersion ||
                (policy.Version == bestVersion && policy.EffectiveFrom > bestEffectiveFrom)));

            if (isBetter)
            {
                highestScore = score;
                bestVersion = policy.Version;
                bestEffectiveFrom = policy.EffectiveFrom;
                bestMatch = new ResolvedCirculationPolicy(
                    PolicyId: policy.Id,
                    PolicyName: policy.Name,
                    Version: policy.Version,
                    IsDefaultFallback: false,
                    MemberGroup: policy.MemberGroup,
                    DocumentType: policy.DocumentType,
                    BranchId: policy.BranchId,
                    MaxLoanBooks: policy.MaxLoanBooks,
                    LoanPeriodDays: policy.LoanPeriodDays,
                    MaxRenewals: policy.MaxRenewals,
                    RenewalPeriodDays: policy.RenewalPeriodDays,
                    HoldDays: policy.HoldDays,
                    BlockIfOverdue: policy.BlockIfOverdue,
                    FinePerDay: policy.FinePerDay,
                    FixedFineAmount: policy.FixedFineAmount,
                    MaxFineAmount: policy.MaxFineAmount,
                    LostBookPenaltyRatio: policy.LostBookPenaltyRatio,
                    MatchScore: score);
            }
        }

        if (bestMatch is not null)
            return bestMatch;

        // Fallback default policy chuẩn của hệ thống
        return new ResolvedCirculationPolicy(
            PolicyId: null,
            PolicyName: "Chính sách mặc định hệ thống",
            Version: 1,
            IsDefaultFallback: true,
            MemberGroup: null,
            DocumentType: null,
            BranchId: null,
            MaxLoanBooks: CirculationPolicy.DefaultMaxLoanBooks,
            LoanPeriodDays: CirculationPolicy.DefaultLoanPeriodDays,
            MaxRenewals: CirculationPolicy.DefaultMaxRenewals,
            RenewalPeriodDays: CirculationPolicy.DefaultRenewalPeriodDays,
            HoldDays: CirculationPolicy.DefaultHoldDays,
            BlockIfOverdue: true,
            FinePerDay: CirculationPolicy.DefaultFinePerDay,
            FixedFineAmount: 0m,
            MaxFineAmount: CirculationPolicy.DefaultMaxFineAmount,
            LostBookPenaltyRatio: CirculationPolicy.DefaultLostBookPenaltyRatio,
            MatchScore: 0);
    }

    public decimal CalculateFine(ResolvedCirculationPolicy policy, int overdueDays)
    {
        if (overdueDays <= 0) return 0m;
        var total = (overdueDays * policy.FinePerDay) + policy.FixedFineAmount;
        if (policy.MaxFineAmount > 0 && total > policy.MaxFineAmount)
            total = policy.MaxFineAmount;
        return Math.Round(total, 2);
    }

    public decimal CalculateLostPenalty(ResolvedCirculationPolicy policy, decimal bookPrice)
    {
        if (bookPrice <= 0) return 0m;
        var ratio = policy.LostBookPenaltyRatio > 0 ? policy.LostBookPenaltyRatio : CirculationPolicy.DefaultLostBookPenaltyRatio;
        return Math.Round(bookPrice * (ratio / 100m), 2);
    }

    public void InvalidateCache()
    {
        lock (CacheLock)
        {
            _cachedPolicies = null;
            _cacheExpiresAtUtc = DateTime.MinValue;
        }
    }

    private async Task<IReadOnlyList<CirculationPolicy>> GetCachedActivePoliciesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        lock (CacheLock)
        {
            if (_cachedPolicies is not null && _cacheExpiresAtUtc > now)
                return _cachedPolicies;
        }

        var list = await repository.GetActivePoliciesAsync(cancellationToken);

        lock (CacheLock)
        {
            _cachedPolicies = list;
            _cacheExpiresAtUtc = now.AddMinutes(5);
        }

        return list;
    }
}
