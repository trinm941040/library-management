namespace UTH.Library.Application.Features.CirculationPolicies;

public sealed record CirculationPolicyModel(
    Guid Id,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    int MaxLoanBooks,
    int LoanPeriodDays,
    int MaxRenewals,
    int RenewalPeriodDays,
    int HoldDays,
    bool BlockIfOverdue,
    decimal FinePerDay,
    decimal FixedFineAmount,
    decimal MaxFineAmount,
    decimal LostBookPenaltyRatio,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    Guid? CreatedByUserId);

public sealed record CirculationPolicyListQuery(
    string? Search,
    bool? IsActive,
    string? MemberGroup,
    Guid? BranchId,
    int PageNumber,
    int PageSize);

public sealed record CirculationPolicyPageModel(
    IReadOnlyList<CirculationPolicyModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateCirculationPolicyCommand(
    string Name,
    string? Description,
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    int MaxLoanBooks,
    int LoanPeriodDays,
    int MaxRenewals,
    int RenewalPeriodDays,
    int HoldDays,
    bool BlockIfOverdue,
    decimal FinePerDay,
    decimal FixedFineAmount,
    decimal MaxFineAmount,
    decimal LostBookPenaltyRatio,
    bool IsActive);

public sealed record UpdateCirculationPolicyCommand(
    string Name,
    string? Description,
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    int MaxLoanBooks,
    int LoanPeriodDays,
    int MaxRenewals,
    int RenewalPeriodDays,
    int HoldDays,
    bool BlockIfOverdue,
    decimal FinePerDay,
    decimal FixedFineAmount,
    decimal MaxFineAmount,
    decimal LostBookPenaltyRatio);

public sealed record CreatePolicyVersionCommand(
    string? Name,
    string? Description,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    int? MaxLoanBooks,
    int? LoanPeriodDays,
    int? MaxRenewals,
    int? RenewalPeriodDays,
    int? HoldDays,
    bool? BlockIfOverdue,
    decimal? FinePerDay,
    decimal? FixedFineAmount,
    decimal? MaxFineAmount,
    decimal? LostBookPenaltyRatio);

public sealed record PolicyPreviewQuery(
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    DateTime? EffectiveAtUtc,
    int? TestOverdueDays,
    decimal? TestBookPrice,
    bool? TestIsLost);

public sealed record ResolvedCirculationPolicy(
    Guid? PolicyId,
    string PolicyName,
    int Version,
    bool IsDefaultFallback,
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    int MaxLoanBooks,
    int LoanPeriodDays,
    int MaxRenewals,
    int RenewalPeriodDays,
    int HoldDays,
    bool BlockIfOverdue,
    decimal FinePerDay,
    decimal FixedFineAmount,
    decimal MaxFineAmount,
    decimal LostBookPenaltyRatio,
    int MatchScore);

public sealed record PolicyPreviewResult(
    ResolvedCirculationPolicy Policy,
    decimal CalculatedOverdueFine,
    decimal CalculatedLostPenalty,
    DateTime SampleDueAtUtc,
    DateTime SampleHoldExpiresAtUtc);

public enum CirculationPolicyFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record CirculationPolicyResult(
    bool Succeeded,
    CirculationPolicyFailure Failure,
    CirculationPolicyModel? Policy,
    IReadOnlyList<string> Errors)
{
    public static CirculationPolicyResult Success(CirculationPolicyModel policy) =>
        new(true, CirculationPolicyFailure.None, policy, []);

    public static CirculationPolicyResult Fail(CirculationPolicyFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
