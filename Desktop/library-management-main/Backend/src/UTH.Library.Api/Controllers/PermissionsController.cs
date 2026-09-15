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
    [ProducesResponseType(typeof(PermissionPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionPageResponse>> Get(
        [FromQuery] PermissionFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await managementService.GetPermissionsAsync(
            request.Search, request.Module, request.PageNumber, request.PageSize, cancellationToken);
        return Ok(new PermissionPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            (int)Math.Ceiling(page.TotalCount / (double)page.PageSize)));
    }

    [HttpGet("modules")]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetModules(CancellationToken cancellationToken) =>
        Ok(await managementService.GetPermissionModulesAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var permission = await managementService.GetPermissionByIdAsync(id, cancellationToken);
        return permission is null ? NotFound(Problem("Không tìm thấy quyền.")) : Ok(ToResponse(permission));
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
        var detail = errors.FirstOrDefault() ?? "Không thể thực hiện thao tác quản lý quyền.";
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
            permission.CreatedAtUtc,
            permission.IsSystem);
}
