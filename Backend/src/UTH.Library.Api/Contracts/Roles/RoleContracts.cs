using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Roles;

public sealed record CreateRoleRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string Description = "");

public sealed record UpdateRoleRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string Description = "",
    bool? IsActive = null);

public sealed record ReplaceRolePermissionsRequest(
    [Required] IReadOnlyCollection<Guid> PermissionIds);

public sealed record ReplaceUserRolesRequest(
    [Required, MinLength(1)] IReadOnlyCollection<Guid> RoleIds);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<PermissionSummaryResponse> Permissions,
    bool IsActive = true);

public sealed record PermissionSummaryResponse(
    Guid Id,
    string Name,
    string Description,
    string Module);
