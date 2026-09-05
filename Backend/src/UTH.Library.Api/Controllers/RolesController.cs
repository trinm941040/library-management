using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Roles;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/roles")]
public sealed class RolesController(IRolePermissionManagementService managementService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var roles = await managementService.GetRolesAsync(search, cancellationToken);
        return Ok(roles.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.RolesRead)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await managementService.GetRoleByIdAsync(id, cancellationToken);
        return role is null ? NotFound(Problem("Role was not found.")) : Ok(ToResponse(role));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesCreate)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.CreateRoleAsync(
            new CreateManagedRoleCommand(request.Name, request.Description),
            cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return MapFailure(result.Failure, result.Errors);

        var response = ToResponse(result.Value);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RolesUpdate)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleResponse>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.UpdateRoleAsync(
            id,
            new UpdateManagedRoleCommand(request.Name, request.Description),
            cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Ok(ToResponse(result.Value))
            : MapFailure(result.Failure, result.Errors);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await managementService.DeleteRoleAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : MapFailure(result.Failure, result.Errors);
    }

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = Permissions.RolesAssign)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleResponse>> ReplacePermissions(
        Guid id,
        [FromBody] ReplaceRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.ReplaceRolePermissionsAsync(id, request.PermissionIds, cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Ok(ToResponse(result.Value))
            : MapFailure(result.Failure, result.Errors);
    }

    [HttpPut("/api/v1/users/{userId:guid}/roles")]
    [Authorize(Policy = Permissions.RolesAssign)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReplaceUserRoles(
        Guid userId,
        [FromBody] ReplaceUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.ReplaceUserRolesAsync(userId, request.RoleIds, cancellationToken);
        return result.Succeeded ? NoContent() : MapFailure(result.Failure, result.Errors);
    }

    private ActionResult MapFailure(
        RolePermissionManagementFailure failure,
        IReadOnlyCollection<string> errors)
    {
        var detail = errors.FirstOrDefault() ?? "Role management operation failed.";
        return failure switch
        {
            RolePermissionManagementFailure.NotFound => NotFound(Problem(detail)),
            RolePermissionManagementFailure.Conflict or RolePermissionManagementFailure.ProtectedResource => Conflict(Problem(detail)),
            _ => BadRequest(Problem(detail))
        };
    }

    private static ProblemDetails Problem(string detail) => new() { Detail = detail };

    private static RoleResponse ToResponse(ManagedRole role) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAtUtc,
            role.Permissions.Select(permission => new PermissionSummaryResponse(
                permission.Id,
                permission.Name,
                permission.Description,
                permission.Module)).ToArray());
}
