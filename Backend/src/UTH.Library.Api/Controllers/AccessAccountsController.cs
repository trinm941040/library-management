using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.AccessAccounts;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/access-accounts")]
public sealed class AccessAccountsController(
    IAccessAccountService accountService,
    IRolePermissionManagementService roleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<ActionResult<AccessAccountPageResponse>> Get(
        [FromQuery] AccessAccountFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await accountService.GetAsync(new AccessAccountListQuery(
            request.Search, request.IsActive, request.PageNumber, request.PageSize), cancellationToken);
        return Ok(new AccessAccountPageResponse(
            page.Items.Select(Map).ToArray(), page.PageNumber, page.PageSize, page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize)));
    }

    [HttpGet("eligible-employees")]
    [Authorize(Policy = Permissions.UsersCreate)]
    public async Task<ActionResult<IReadOnlyCollection<EligibleAccessAccountEmployeeResponse>>> GetEligibleEmployees(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        Ok((await accountService.GetEligibleEmployeesAsync(search, cancellationToken))
            .Select(value => new EligibleAccessAccountEmployeeResponse(
                value.Id, value.EmployeeCode, value.FullName, value.Email)).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<ActionResult<AccessAccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountService.GetByIdAsync(id, cancellationToken);
        return account is null ? NotFound(Problem("Không tìm thấy tài khoản truy cập.")) : Ok(Map(account));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.UsersCreate)]
    public async Task<ActionResult<AccessAccountResponse>> Create(
        CreateAccessAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.CreateAsync(new CreateAccessAccountCommand(
            request.EmployeeId, request.Email, request.DisplayName, request.Password,
            request.RoleIds, CurrentUserId()), cancellationToken);
        if (!result.Succeeded || result.Account is null) return Failure(result);
        var response = Map(result.Account);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.UsersDeactivate)]
    public async Task<ActionResult<AccessAccountResponse>> SetStatus(
        Guid id,
        SetAccessAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.SetStatusAsync(id, request.IsActive, CurrentUserId(), cancellationToken);
        return result.Succeeded && result.Account is not null ? Ok(Map(result.Account)) : Failure(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = Permissions.UsersUpdate)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        ResetAccessAccountPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.ResetPasswordAsync(
            id, request.TemporaryPassword, CurrentUserId(), cancellationToken);
        return result.Succeeded ? NoContent() : Failure(result);
    }

    [HttpGet("{id:guid}/sessions")]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<ActionResult<IReadOnlyCollection<AccessAccountSessionResponse>>> GetSessions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sessions = await accountService.GetSessionsAsync(id, cancellationToken);
        return sessions is null
            ? NotFound(Problem("Không tìm thấy tài khoản truy cập."))
            : Ok(sessions.Select(value => new AccessAccountSessionResponse(
                value.Id, value.CreatedAtUtc, value.ExpiresAtUtc, value.UsedAtUtc,
                value.RevokedAtUtc, value.CreatedByIp, value.UserAgent,
                value.RevocationReason, value.IsActive)).ToArray());
    }

    [HttpDelete("{id:guid}/sessions/{sessionId:guid}")]
    [Authorize(Policy = Permissions.UsersUpdate)]
    public async Task<IActionResult> RevokeSession(
        Guid id,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await accountService.RevokeSessionAsync(id, sessionId, CurrentUserId(), cancellationToken);
        return result.Succeeded ? NoContent() : Failure(result);
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = Permissions.RolesAssign)]
    public async Task<IActionResult> ReplaceRoles(
        Guid id,
        ReplaceAccessAccountRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (await accountService.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(Problem("Không tìm thấy tài khoản truy cập."));
        var result = await roleService.ReplaceUserRolesAsync(id, request.RoleIds, cancellationToken);
        if (result.Succeeded) return NoContent();
        var mapped = result.Failure switch
        {
            RolePermissionManagementFailure.NotFound => AccessAccountFailure.NotFound,
            RolePermissionManagementFailure.Conflict => AccessAccountFailure.Conflict,
            RolePermissionManagementFailure.ProtectedResource => AccessAccountFailure.ProtectedResource,
            _ => AccessAccountFailure.Validation
        };
        return Failure(AccessAccountResult.Failed(mapped, result.Errors.ToArray()));
    }

    private ActionResult Failure(AccessAccountResult result)
    {
        var detail = result.Errors.FirstOrDefault() ?? "Không thể thực hiện thao tác tài khoản.";
        return result.Failure switch
        {
            AccessAccountFailure.NotFound => NotFound(Problem(detail)),
            AccessAccountFailure.Conflict or AccessAccountFailure.ProtectedResource => Conflict(Problem(detail)),
            _ => UnprocessableEntity(Problem(detail))
        };
    }

    private Guid CurrentUserId() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedAccessException("Không xác định được tài khoản hiện tại.");

    private static ProblemDetails Problem(string detail) => new() { Detail = detail };
    private static AccessAccountResponse Map(AccessAccount value) => new(
        value.Id, value.Email, value.DisplayName, value.IsActive, value.EmailConfirmed,
        value.CreatedAtUtc, value.LastLoginAtUtc, value.Roles,
        new AccessAccountEmployeeResponse(value.Employee.Id, value.Employee.EmployeeCode,
            value.Employee.FullName, value.Employee.Email, value.Employee.EmploymentStatus),
        value.IsProtected);
}
