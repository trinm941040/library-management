using UTH.Library.Application.Features.CirculationPolicies;

namespace UTH.Library.Api.Contracts.CirculationPolicies;

public sealed record CirculationPolicyResponse(
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

public sealed record CirculationPolicyPageResponse(
    IReadOnlyList<CirculationPolicyResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreateCirculationPolicyRequest(
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

public sealed record UpdateCirculationPolicyRequest(
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

public sealed record CreatePolicyVersionRequest(
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

public sealed record PolicyPreviewRequest(
    string? MemberGroup,
    string? DocumentType,
    Guid? BranchId,
    DateTime? EffectiveAtUtc,
    int? TestOverdueDays,
    decimal? TestBookPrice,
    bool? TestIsLost);

public sealed record PolicyPreviewResponse(
    ResolvedCirculationPolicy Policy,
    decimal CalculatedOverdueFine,
    decimal CalculatedLostPenalty,
    DateTime SampleDueAtUtc,
    DateTime SampleHoldExpiresAtUtc);
