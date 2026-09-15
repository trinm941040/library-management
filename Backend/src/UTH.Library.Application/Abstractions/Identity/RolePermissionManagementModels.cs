namespace UTH.Library.Application.Abstractions.Identity;

public sealed record ManagedPermission(
    Guid Id,
    string Name,
    string Description,
    string Module,
    DateTime CreatedAtUtc,
    bool IsSystem);

public sealed record ManagedRole(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ManagedPermission> Permissions,
    bool IsActive = true);

public sealed record ManagedPage<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateManagedRoleCommand(string Name, string Description);
public sealed record UpdateManagedRoleCommand(string Name, string Description, bool? IsActive = null);
public sealed record CreateManagedPermissionCommand(string Name, string Description, string Module);
public sealed record UpdateManagedPermissionCommand(string Name, string Description, string Module);

public enum RolePermissionManagementFailure
{
    None,
    NotFound,
    Conflict,
    Validation,
    ProtectedResource
}

public sealed record RolePermissionManagementResult<T>(
    T? Value,
    RolePermissionManagementFailure Failure,
    IReadOnlyCollection<string> Errors)
    where T : class
{
    public bool Succeeded => Failure == RolePermissionManagementFailure.None;

    public static RolePermissionManagementResult<T> Success(T value) =>
        new(value, RolePermissionManagementFailure.None, []);

    public static RolePermissionManagementResult<T> Failed(
        RolePermissionManagementFailure failure,
        params string[] errors) =>
        new(null, failure, errors);
}

public sealed record RolePermissionOperationResult(
    RolePermissionManagementFailure Failure,
    IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Failure == RolePermissionManagementFailure.None;

    public static RolePermissionOperationResult Success() =>
        new(RolePermissionManagementFailure.None, []);

    public static RolePermissionOperationResult Failed(
        RolePermissionManagementFailure failure,
        params string[] errors) =>
        new(failure, errors);
}

public interface IRolePermissionManagementService
{
    Task<ManagedPage<ManagedRole>> GetRolesAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ManagedRole?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RolePermissionManagementResult<ManagedRole>> CreateRoleAsync(CreateManagedRoleCommand command, CancellationToken cancellationToken);
    Task<RolePermissionManagementResult<ManagedRole>> UpdateRoleAsync(Guid id, UpdateManagedRoleCommand command, CancellationToken cancellationToken);
    Task<RolePermissionOperationResult> DeleteRoleAsync(Guid id, CancellationToken cancellationToken);
    Task<RolePermissionManagementResult<ManagedRole>> ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken);
    Task<RolePermissionOperationResult> ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);

    Task<ManagedPage<ManagedPermission>> GetPermissionsAsync(string? search, string? module, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetPermissionModulesAsync(CancellationToken cancellationToken);
    Task<ManagedPermission?> GetPermissionByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RolePermissionManagementResult<ManagedPermission>> CreatePermissionAsync(CreateManagedPermissionCommand command, CancellationToken cancellationToken);
    Task<RolePermissionManagementResult<ManagedPermission>> UpdatePermissionAsync(Guid id, UpdateManagedPermissionCommand command, CancellationToken cancellationToken);
    Task<RolePermissionOperationResult> DeletePermissionAsync(Guid id, CancellationToken cancellationToken);
}
