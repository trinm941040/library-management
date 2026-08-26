using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Permissions;

public sealed record CreatePermissionRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Name,
    [StringLength(500)] string Description,
    [Required, StringLength(100, MinimumLength = 1)] string Module);

public sealed record UpdatePermissionRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Name,
    [StringLength(500)] string Description,
    [Required, StringLength(100, MinimumLength = 1)] string Module);

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string Description,
    string Module,
    DateTime CreatedAtUtc);
