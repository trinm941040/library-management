using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.CirculationPolicies;

public sealed class CirculationPolicyService(
    ICirculationPolicyRepository repository,
    ICirculationPolicyResolver resolver,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<CirculationPolicyPageModel> GetPageAsync(
        CirculationPolicyListQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await repository.GetPageAsync(
            query.Search,
            query.IsActive,
            query.MemberGroup,
            query.BranchId,
            pageNumber,
            pageSize,
            cancellationToken);

        return new CirculationPolicyPageModel(
            items.Select(Map).ToArray(),
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<CirculationPolicyModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(id, cancellationToken);
        return policy is null ? null : Map(policy);
    }

    public async Task<CirculationPolicyResult> CreateAsync(
        CreateCirculationPolicyCommand command,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (command.IsActive)
        {
            var hasOverlap = await repository.HasOverlappingActivePolicyAsync(
                null,
                command.MemberGroup,
                command.DocumentType,
                command.BranchId,
                command.EffectiveFrom,
                command.EffectiveTo,
                cancellationToken);

            if (hasOverlap)
            {
                return CirculationPolicyResult.Fail(
                    CirculationPolicyFailure.Conflict,
                    "Không thể tạo chính sách ở trạng thái Active: Đã tồn tại chính sách đang hoạt động có cùng phạm vi và trùng lặp thời gian hiệu lực.");
            }
        }

        try
        {
            var policy = CirculationPolicy.Create(
                command.Name,
                command.Description,
                command.MemberGroup,
                command.DocumentType,
                command.BranchId,
                command.EffectiveFrom,
                command.EffectiveTo,
                command.MaxLoanBooks,
                command.LoanPeriodDays,
                command.MaxRenewals,
                command.RenewalPeriodDays,
                command.HoldDays,
                command.BlockIfOverdue,
                command.FinePerDay,
                command.FixedFineAmount,
                command.MaxFineAmount,
                command.LostBookPenaltyRatio,
                command.IsActive,
                actorUserId,
                now);

            await repository.AddAsync(policy, cancellationToken);

            var afterJson = JsonSerializer.Serialize(Map(policy), JsonOptions);
            await repository.AddAuditLogAsync(
                AuditLog.Create(
                    actorUserId,
                    "circulation-policy.created",
                    nameof(CirculationPolicy),
                    policy.Id,
                    null,
                    afterJson,
                    now),
                cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);
            resolver.InvalidateCache();

            return CirculationPolicyResult.Success(Map(policy));
        }
        catch (ArgumentException exception)
        {
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.Validation, exception.Message);
        }
    }

    public async Task<CirculationPolicyResult> UpdateAsync(
        Guid id,
        UpdateCirculationPolicyCommand command,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.NotFound, "Chính sách không tồn tại.");

        if (policy.IsActive)
        {
            var hasOverlap = await repository.HasOverlappingActivePolicyAsync(
                policy.Id,
                command.MemberGroup,
                command.DocumentType,
                command.BranchId,
                command.EffectiveFrom,
                command.EffectiveTo,
                cancellationToken);

            if (hasOverlap)
            {
                return CirculationPolicyResult.Fail(
                    CirculationPolicyFailure.Conflict,
                    "Không thể cập nhật: Thay đổi thời gian hoặc phạm vi gây trùng lặp với một chính sách đang hoạt động khác.");
            }
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var beforeJson = JsonSerializer.Serialize(Map(policy), JsonOptions);

        try
        {
            policy.Update(
                command.Name,
                command.Description,
                command.MemberGroup,
                command.DocumentType,
                command.BranchId,
                command.EffectiveFrom,
                command.EffectiveTo,
                command.MaxLoanBooks,
                command.LoanPeriodDays,
                command.MaxRenewals,
                command.RenewalPeriodDays,
                command.HoldDays,
                command.BlockIfOverdue,
                command.FinePerDay,
                command.FixedFineAmount,
                command.MaxFineAmount,
                command.LostBookPenaltyRatio,
                now);

            var afterJson = JsonSerializer.Serialize(Map(policy), JsonOptions);
            await repository.AddAuditLogAsync(
                AuditLog.Create(
                    actorUserId,
                    "circulation-policy.updated",
                    nameof(CirculationPolicy),
                    policy.Id,
                    beforeJson,
                    afterJson,
                    now),
                cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);
            resolver.InvalidateCache();

            return CirculationPolicyResult.Success(Map(policy));
        }
        catch (ArgumentException exception)
        {
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.Validation, exception.Message);
        }
    }

    public async Task<CirculationPolicyResult> CreateVersionAsync(
        Guid id,
        CreatePolicyVersionCommand command,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var basePolicy = await repository.GetByIdAsync(id, cancellationToken);
        if (basePolicy is null)
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.NotFound, "Chính sách gốc không tồn tại.");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var newPolicy = basePolicy.CreateNextVersion(
                command.Name,
                command.Description,
                command.EffectiveFrom,
                command.EffectiveTo,
                command.MaxLoanBooks,
                command.LoanPeriodDays,
                command.MaxRenewals,
                command.RenewalPeriodDays,
                command.HoldDays,
                command.BlockIfOverdue,
                command.FinePerDay,
                command.FixedFineAmount,
                command.MaxFineAmount,
                command.LostBookPenaltyRatio,
                actorUserId,
                now);

            await repository.AddAsync(newPolicy, cancellationToken);

            var afterJson = JsonSerializer.Serialize(Map(newPolicy), JsonOptions);
            await repository.AddAuditLogAsync(
                AuditLog.Create(
                    actorUserId,
                    "circulation-policy.version-created",
                    nameof(CirculationPolicy),
                    newPolicy.Id,
                    null,
                    afterJson,
                    now),
                cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);
            return CirculationPolicyResult.Success(Map(newPolicy));
        }
        catch (ArgumentException exception)
        {
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.Validation, exception.Message);
        }
    }

    public async Task<CirculationPolicyResult> ActivateAsync(
        Guid id,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.NotFound, "Chính sách không tồn tại.");

        if (policy.IsActive)
            return CirculationPolicyResult.Success(Map(policy));

        var hasOverlap = await repository.HasOverlappingActivePolicyAsync(
            policy.Id,
            policy.MemberGroup,
            policy.DocumentType,
            policy.BranchId,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            cancellationToken);

        if (hasOverlap)
        {
            return CirculationPolicyResult.Fail(
                CirculationPolicyFailure.Conflict,
                "Không thể kích hoạt: Đã có chính sách đang hoạt động có cùng phạm vi và trùng lặp thời gian hiệu lực.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var beforeJson = JsonSerializer.Serialize(Map(policy), JsonOptions);

        policy.Activate(now);

        var afterJson = JsonSerializer.Serialize(Map(policy), JsonOptions);
        await repository.AddAuditLogAsync(
            AuditLog.Create(
                actorUserId,
                "circulation-policy.activated",
                nameof(CirculationPolicy),
                policy.Id,
                beforeJson,
                afterJson,
                now),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        resolver.InvalidateCache();

        return CirculationPolicyResult.Success(Map(policy));
    }

    public async Task<CirculationPolicyResult> DeactivateAsync(
        Guid id,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
            return CirculationPolicyResult.Fail(CirculationPolicyFailure.NotFound, "Chính sách không tồn tại.");

        if (!policy.IsActive)
            return CirculationPolicyResult.Success(Map(policy));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var beforeJson = JsonSerializer.Serialize(Map(policy), JsonOptions);

        policy.Deactivate(now);

        var afterJson = JsonSerializer.Serialize(Map(policy), JsonOptions);
        await repository.AddAuditLogAsync(
            AuditLog.Create(
                actorUserId,
                "circulation-policy.deactivated",
                nameof(CirculationPolicy),
                policy.Id,
                beforeJson,
                afterJson,
                now),
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        resolver.InvalidateCache();

        return CirculationPolicyResult.Success(Map(policy));
    }

    public async Task<PolicyPreviewResult> PreviewAsync(
        PolicyPreviewQuery query,
        CancellationToken cancellationToken)
    {
        var resolved = await resolver.ResolveAsync(
            query.MemberGroup,
            query.DocumentType,
            query.BranchId,
            query.EffectiveAtUtc,
            cancellationToken);

        var now = query.EffectiveAtUtc ?? timeProvider.GetUtcNow().UtcDateTime;
        var testOverdue = query.TestOverdueDays ?? 3;
        var testPrice = query.TestBookPrice ?? 100000m;

        var overdueFine = resolver.CalculateFine(resolved, testOverdue);
        var lostPenalty = resolver.CalculateLostPenalty(resolved, testPrice);

        return new PolicyPreviewResult(
            Policy: resolved,
            CalculatedOverdueFine: overdueFine,
            CalculatedLostPenalty: lostPenalty,
            SampleDueAtUtc: now.AddDays(resolved.LoanPeriodDays),
            SampleHoldExpiresAtUtc: now.AddDays(resolved.HoldDays));
    }

    private static CirculationPolicyModel Map(CirculationPolicy policy) =>
        new(
            policy.Id,
            policy.Name,
            policy.Description,
            policy.Version,
            policy.IsActive,
            policy.MemberGroup,
            policy.DocumentType,
            policy.BranchId,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            policy.MaxLoanBooks,
            policy.LoanPeriodDays,
            policy.MaxRenewals,
            policy.RenewalPeriodDays,
            policy.HoldDays,
            policy.BlockIfOverdue,
            policy.FinePerDay,
            policy.FixedFineAmount,
            policy.MaxFineAmount,
            policy.LostBookPenaltyRatio,
            policy.CreatedAtUtc,
            policy.UpdatedAtUtc,
            policy.CreatedByUserId);
}
