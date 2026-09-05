using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Permissions;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/permissions")]
public sealed class PermissionsController(IRolePermissionManagementService managementService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? module,
        CancellationToken cancellationToken)
    {
        var permissions = await managementService.GetPermissionsAsync(search, module, cancellationToken);
        return Ok(permissions.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var permission = await managementService.GetPermissionByIdAsync(id, cancellationToken);
        return permission is null ? NotFound(Problem("Permission was not found.")) : Ok(ToResponse(permission));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PermissionsCreate)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PermissionResponse>> Create(
        [FromBody] CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.CreatePermissionAsync(
            new CreateManagedPermissionCommand(request.Name, request.Description, request.Module),
            cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return MapFailure(result.Failure, result.Errors);

        var response = ToResponse(result.Value);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PermissionsUpdate)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionResponse>> Update(
        Guid id,
        [FromBody] UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.UpdatePermissionAsync(
            id,
            new UpdateManagedPermissionCommand(request.Name, request.Description, request.Module),
            cancellationToken);
        return result.Succeeded && result.Value is not null
            ? Ok(ToResponse(result.Value))
            : MapFailure(result.Failure, result.Errors);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.PermissionsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await managementService.DeletePermissionAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : MapFailure(result.Failure, result.Errors);
    }

    private ActionResult MapFailure(
        RolePermissionManagementFailure failure,
        IReadOnlyCollection<string> errors)
    {
        var detail = errors.FirstOrDefault() ?? "Permission management operation failed.";
        return failure switch
        {
            RolePermissionManagementFailure.NotFound => NotFound(Problem(detail)),
            RolePermissionManagementFailure.Conflict or RolePermissionManagementFailure.ProtectedResource => Conflict(Problem(detail)),
            _ => BadRequest(Problem(detail))
        };
    }

    private static ProblemDetails Problem(string detail) => new() { Detail = detail };

    private static PermissionResponse ToResponse(ManagedPermission permission) =>
        new(
            permission.Id,
            permission.Name,
            permission.Description,
            permission.Module,
            permission.CreatedAtUtc);
}
