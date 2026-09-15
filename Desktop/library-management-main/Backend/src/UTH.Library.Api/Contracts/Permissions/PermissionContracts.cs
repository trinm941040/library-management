using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Permissions;

public sealed class PermissionFilterRequest
{
    public string? Search { get; init; }
    public string? Module { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

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
    DateTime CreatedAtUtc,
    bool IsSystem);

public sealed record PermissionPageResponse(
    IReadOnlyCollection<PermissionResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
