using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.CirculationPolicies;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.CirculationPolicies;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/circulation-policies")]
public sealed class CirculationPoliciesController(CirculationPolicyService policyService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CirculationPoliciesRead)]
    [ProducesResponseType(typeof(CirculationPolicyPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CirculationPolicyPageResponse>> Get(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? memberGroup,
        [FromQuery] Guid? branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await policyService.GetPageAsync(
            new CirculationPolicyListQuery(search, isActive, memberGroup, branchId, pageNumber, pageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new CirculationPolicyPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CirculationPoliciesRead)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CirculationPolicyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var policy = await policyService.GetByIdAsync(id, cancellationToken);
        return policy is null
            ? NotFound(new ProblemDetails { Title = "Không tìm thấy", Detail = "Chính sách không tồn tại." })
            : Ok(ToResponse(policy));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CirculationPoliciesManage)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CirculationPolicyResponse>> Create(
        [FromBody] CreateCirculationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCirculationPolicyCommand(
            request.Name,
            request.Description,
            request.MemberGroup,
            request.DocumentType,
            request.BranchId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.MaxLoanBooks,
            request.LoanPeriodDays,
            request.MaxRenewals,
            request.RenewalPeriodDays,
            request.HoldDays,
            request.BlockIfOverdue,
            request.FinePerDay,
            request.FixedFineAmount,
            request.MaxFineAmount,
            request.LostBookPenaltyRatio,
            request.IsActive);

        var result = await policyService.CreateAsync(command, GetCurrentUserId(), cancellationToken);
        if (!result.Succeeded || result.Policy is null)
            return MapFailure(result);

        var response = ToResponse(result.Policy);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CirculationPoliciesManage)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CirculationPolicyResponse>> Update(
        Guid id,
        [FromBody] UpdateCirculationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCirculationPolicyCommand(
            request.ConcurrencyToken,
            request.Name,
            request.Description,
            request.MemberGroup,
            request.DocumentType,
            request.BranchId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.MaxLoanBooks,
            request.LoanPeriodDays,
            request.MaxRenewals,
            request.RenewalPeriodDays,
            request.HoldDays,
            request.BlockIfOverdue,
            request.FinePerDay,
            request.FixedFineAmount,
            request.MaxFineAmount,
            request.LostBookPenaltyRatio);

        var result = await policyService.UpdateAsync(id, command, GetCurrentUserId(), cancellationToken);
        if (!result.Succeeded || result.Policy is null)
            return MapFailure(result);

        return Ok(ToResponse(result.Policy));
    }

    [HttpPost("{id:guid}/versions")]
    [Authorize(Policy = Permissions.CirculationPoliciesManage)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CirculationPolicyResponse>> CreateVersion(
        Guid id,
        [FromBody] CreatePolicyVersionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePolicyVersionCommand(
            request.ConcurrencyToken,
            request.Name,
            request.Description,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.MaxLoanBooks,
            request.LoanPeriodDays,
            request.MaxRenewals,
            request.RenewalPeriodDays,
            request.HoldDays,
            request.BlockIfOverdue,
            request.FinePerDay,
            request.FixedFineAmount,
            request.MaxFineAmount,
            request.LostBookPenaltyRatio);

        var result = await policyService.CreateVersionAsync(id, command, GetCurrentUserId(), cancellationToken);
        if (!result.Succeeded || result.Policy is null)
            return MapFailure(result);

        var response = ToResponse(result.Policy);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.CirculationPoliciesManage)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CirculationPolicyResponse>> Activate(
        Guid id,
        [FromBody] ChangePolicyStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await policyService.ActivateAsync(id, request.ConcurrencyToken, GetCurrentUserId(), cancellationToken);
        if (!result.Succeeded || result.Policy is null)
            return MapFailure(result);

        return Ok(ToResponse(result.Policy));
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.CirculationPoliciesManage)]
    [ProducesResponseType(typeof(CirculationPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CirculationPolicyResponse>> Deactivate(
        Guid id,
        [FromBody] ChangePolicyStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await policyService.DeactivateAsync(id, request.ConcurrencyToken, GetCurrentUserId(), cancellationToken);
        if (!result.Succeeded || result.Policy is null)
            return MapFailure(result);

        return Ok(ToResponse(result.Policy));
    }

    [HttpPost("preview")]
    [Authorize(Policy = Permissions.CirculationPoliciesRead)]
    [ProducesResponseType(typeof(PolicyPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PolicyPreviewResponse>> Preview(
        [FromBody] PolicyPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await policyService.PreviewAsync(
            new PolicyPreviewQuery(
                request.MemberGroup,
                request.DocumentType,
                request.BranchId,
                request.EffectiveAtUtc,
                request.TestOverdueDays,
                request.TestBookPrice,
                request.TestIsLost),
            cancellationToken);

        return Ok(new PolicyPreviewResponse(
            result.Policy,
            result.CalculatedOverdueFine,
            result.CalculatedLostPenalty,
            result.SampleDueAtUtc,
            result.SampleHoldExpiresAtUtc));
    }

    private ActionResult MapFailure(CirculationPolicyResult result) =>
        result.Failure switch
        {
            CirculationPolicyFailure.NotFound => NotFound(new ProblemDetails
            {
                Title = "Không tìm thấy",
                Detail = result.Errors.FirstOrDefault() ?? "Chính sách không tìm thấy."
            }),
            CirculationPolicyFailure.Conflict => Conflict(new ProblemDetails
            {
                Title = "Xung đột dữ liệu",
                Detail = result.Errors.FirstOrDefault() ?? "Xung đột chính sách."
            }),
            _ => BadRequest(new ProblemDetails
            {
                Title = "Dữ liệu không hợp lệ",
                Detail = result.Errors.FirstOrDefault() ?? "Dữ liệu không hợp lệ."
            })
        };

    private static CirculationPolicyResponse ToResponse(CirculationPolicyModel policy) =>
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
            policy.CreatedByUserId,
            policy.ConcurrencyToken);

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : null;
}
