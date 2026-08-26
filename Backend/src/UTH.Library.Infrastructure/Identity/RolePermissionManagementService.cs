using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class RolePermissionManagementService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    LibraryDbContext db,
    TimeProvider timeProvider) : IRolePermissionManagementService
{
    private static readonly Regex PermissionNamePattern = new(
        "^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<IReadOnlyCollection<ManagedRole>> GetRolesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();
            query = query.Where(role =>
                (role.NormalizedName != null && role.NormalizedName.Contains(value)) ||
                role.Description.ToUpper().Contains(value));
        }

        var roles = await query.OrderBy(role => role.Name).ToListAsync(cancellationToken);
        return await MapRolesAsync(roles, cancellationToken);
    }

    public async Task<ManagedRole?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (role is null)
            return null;

        return (await MapRolesAsync([role], cancellationToken)).Single();
    }

    public async Task<RolePermissionManagementResult<ManagedRole>> CreateRoleAsync(
        CreateManagedRoleCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (await roleManager.FindByNameAsync(name) is not null)
            return RoleFailure(RolePermissionManagementFailure.Conflict, "A role with this name already exists.");

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = command.Description.Trim(),
            IsSystemRole = false,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return RoleFailure(result);

        return RolePermissionManagementResult<ManagedRole>.Success(MapRole(role, []));
    }

    public async Task<RolePermissionManagementResult<ManagedRole>> UpdateRoleAsync(
        Guid id,
        UpdateManagedRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return RoleFailure(RolePermissionManagementFailure.NotFound, "Role was not found.");

        var name = command.Name.Trim();
        if (role.IsSystemRole && !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
            return RoleFailure(RolePermissionManagementFailure.ProtectedResource, "A system role cannot be renamed.");

        var duplicate = await roleManager.FindByNameAsync(name);
        if (duplicate is not null && duplicate.Id != id)
            return RoleFailure(RolePermissionManagementFailure.Conflict, "A role with this name already exists.");

        role.Name = name;
        role.Description = command.Description.Trim();
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return RoleFailure(result);

        var mapped = await GetRoleByIdAsync(role.Id, cancellationToken);
        return RolePermissionManagementResult<ManagedRole>.Success(mapped!);
    }

    public async Task<RolePermissionOperationResult> DeleteRoleAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return OperationFailure(RolePermissionManagementFailure.NotFound, "Role was not found.");
        if (role.IsSystemRole)
            return OperationFailure(RolePermissionManagementFailure.ProtectedResource, "A system role cannot be deleted.");
        if (await db.UserRoles.AnyAsync(value => value.RoleId == id, cancellationToken))
            return OperationFailure(RolePermissionManagementFailure.Conflict, "The role is assigned to one or more users.");

        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded ? RolePermissionOperationResult.Success() : OperationFailure(result);
    }

    public async Task<RolePermissionManagementResult<ManagedRole>> ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(value => value.Id == roleId, cancellationToken);
        if (role is null)
            return RoleFailure(RolePermissionManagementFailure.NotFound, "Role was not found.");

        var requestedIds = permissionIds.Distinct().ToArray();
        var permissions = await db.Permissions
            .Where(permission => requestedIds.Contains(permission.Id))
            .OrderBy(permission => permission.Name)
            .ToListAsync(cancellationToken);
        if (permissions.Count != requestedIds.Length)
            return RoleFailure(RolePermissionManagementFailure.Validation, "One or more permissions do not exist.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.RolePermissions
            .Where(value => value.RoleId == roleId)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(value => value.PermissionId).ToHashSet();
        var requestedIdSet = requestedIds.ToHashSet();
        db.RolePermissions.RemoveRange(existing.Where(value => !requestedIdSet.Contains(value.PermissionId)));
        db.RolePermissions.AddRange(requestedIds.Where(permissionId => !existingIds.Contains(permissionId)).Select(permissionId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RolePermissionManagementResult<ManagedRole>.Success(MapRole(role, permissions));
    }

    public async Task<RolePermissionOperationResult> ReplaceUserRolesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return OperationFailure(RolePermissionManagementFailure.NotFound, "User was not found.");

        var requestedIds = roleIds.Distinct().ToArray();
        if (requestedIds.Length == 0)
            return OperationFailure(RolePermissionManagementFailure.Validation, "At least one role is required.");

        var requestedRoles = await db.Roles
            .Where(role => requestedIds.Contains(role.Id))
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
        if (requestedRoles.Count != requestedIds.Length)
            return OperationFailure(RolePermissionManagementFailure.Validation, "One or more roles do not exist.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase))
            return OperationFailure(
                RolePermissionManagementFailure.ProtectedResource,
                "Administrator accounts cannot have their roles changed.");

        var requestedNames = requestedRoles.Select(role => role.Name!).ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var rolesToRemove = currentRoles.Except(requestedNames, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToAdd = requestedNames.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                return OperationFailure(removeResult);
        }
        if (rolesToAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
                return OperationFailure(addResult);
        }

        if (rolesToRemove.Length > 0 || rolesToAdd.Length > 0)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return OperationFailure(stampResult);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            await db.RefreshTokenSessions
                .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.RevokedAtUtc, now)
                        .SetProperty(token => token.RevocationReason, "user-roles-updated"),
                    cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return RolePermissionOperationResult.Success();
    }

    public async Task<IReadOnlyCollection<ManagedPermission>> GetPermissionsAsync(
        string? search,
        string? module,
        CancellationToken cancellationToken)
    {
        var query = db.Permissions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();
            query = query.Where(permission =>
                permission.Name.ToUpper().Contains(value) ||
                permission.Description.ToUpper().Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(module))
        {
            var value = module.Trim().ToUpperInvariant();
            query = query.Where(permission => permission.Module.ToUpper() == value);
        }

        return await query
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Name)
            .Select(permission => MapPermission(permission))
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagedPermission?> GetPermissionByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking()
            .Where(permission => permission.Id == id)
            .Select(permission => new ManagedPermission(
                permission.Id,
                permission.Name,
                permission.Description,
                permission.Module,
                permission.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RolePermissionManagementResult<ManagedPermission>> CreatePermissionAsync(
        CreateManagedPermissionCommand command,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizePermission(command.Name, command.Module);
        if (normalized.Error is not null)
            return PermissionFailure(RolePermissionManagementFailure.Validation, normalized.Error);
        if (await db.Permissions.AnyAsync(value => value.Name == normalized.Name, cancellationToken))
            return PermissionFailure(RolePermissionManagementFailure.Conflict, "A permission with this name already exists.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = normalized.Name,
            Description = command.Description.Trim(),
            Module = normalized.Module,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(cancellationToken);
        return RolePermissionManagementResult<ManagedPermission>.Success(MapPermission(permission));
    }

    public async Task<RolePermissionManagementResult<ManagedPermission>> UpdatePermissionAsync(
        Guid id,
        UpdateManagedPermissionCommand command,
        CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (permission is null)
            return PermissionFailure(RolePermissionManagementFailure.NotFound, "Permission was not found.");

        var normalized = NormalizePermission(command.Name, command.Module);
        if (normalized.Error is not null)
            return PermissionFailure(RolePermissionManagementFailure.Validation, normalized.Error);
        if (Permissions.All.Contains(permission.Name) &&
            (!string.Equals(permission.Name, normalized.Name, StringComparison.Ordinal) ||
             !string.Equals(permission.Module, normalized.Module, StringComparison.Ordinal)))
            return PermissionFailure(RolePermissionManagementFailure.ProtectedResource, "A system permission cannot be renamed or moved to another module.");
        if (await db.Permissions.AnyAsync(value => value.Id != id && value.Name == normalized.Name, cancellationToken))
            return PermissionFailure(RolePermissionManagementFailure.Conflict, "A permission with this name already exists.");

        permission.Name = normalized.Name;
        permission.Module = normalized.Module;
        permission.Description = command.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return RolePermissionManagementResult<ManagedPermission>.Success(MapPermission(permission));
    }

    public async Task<RolePermissionOperationResult> DeletePermissionAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (permission is null)
            return OperationFailure(RolePermissionManagementFailure.NotFound, "Permission was not found.");
        if (Permissions.All.Contains(permission.Name))
            return OperationFailure(RolePermissionManagementFailure.ProtectedResource, "A system permission cannot be deleted.");

        db.Permissions.Remove(permission);
        await db.SaveChangesAsync(cancellationToken);
        return RolePermissionOperationResult.Success();
    }

    private async Task<IReadOnlyCollection<ManagedRole>> MapRolesAsync(
        IReadOnlyCollection<ApplicationRole> roles,
        CancellationToken cancellationToken)
    {
        var roleIds = roles.Select(role => role.Id).ToArray();
        var assignments = await db.RolePermissions.AsNoTracking()
            .Where(value => roleIds.Contains(value.RoleId))
            .Include(value => value.Permission)
            .ToListAsync(cancellationToken);
        var permissionsByRole = assignments
            .GroupBy(value => value.RoleId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<Permission>)group.Select(value => value.Permission).OrderBy(value => value.Name).ToArray());

        return roles.Select(role => MapRole(role, permissionsByRole.GetValueOrDefault(role.Id, []))).ToArray();
    }

    private static ManagedRole MapRole(ApplicationRole role, IEnumerable<Permission> permissions) =>
        new(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            role.IsSystemRole,
            role.CreatedAtUtc,
            permissions.Select(MapPermission).ToArray());

    private static ManagedPermission MapPermission(Permission permission) =>
        new(permission.Id, permission.Name, permission.Description, permission.Module, permission.CreatedAtUtc);

    private static (string Name, string Module, string? Error) NormalizePermission(string name, string module)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var normalizedModule = module.Trim().ToLowerInvariant();
        if (!PermissionNamePattern.IsMatch(normalizedName))
            return (normalizedName, normalizedModule, "Permission name must use the 'module.action' format with lowercase letters, numbers, or hyphens.");
        if (!normalizedName.StartsWith($"{normalizedModule}.", StringComparison.Ordinal))
            return (normalizedName, normalizedModule, "Permission module must match the module prefix in the permission name.");
        return (normalizedName, normalizedModule, null);
    }

    private static RolePermissionManagementResult<ManagedRole> RoleFailure(
        RolePermissionManagementFailure failure,
        string error) => RolePermissionManagementResult<ManagedRole>.Failed(failure, error);

    private static RolePermissionManagementResult<ManagedRole> RoleFailure(IdentityResult result) =>
        RolePermissionManagementResult<ManagedRole>.Failed(
            result.Errors.Any(error => error.Code == "DuplicateRoleName")
                ? RolePermissionManagementFailure.Conflict
                : RolePermissionManagementFailure.Validation,
            result.Errors.Select(error => error.Description).Distinct().ToArray());

    private static RolePermissionManagementResult<ManagedPermission> PermissionFailure(
        RolePermissionManagementFailure failure,
        string error) => RolePermissionManagementResult<ManagedPermission>.Failed(failure, error);

    private static RolePermissionOperationResult OperationFailure(
        RolePermissionManagementFailure failure,
        string error) => RolePermissionOperationResult.Failed(failure, error);

    private static RolePermissionOperationResult OperationFailure(IdentityResult result) =>
        RolePermissionOperationResult.Failed(
            RolePermissionManagementFailure.Validation,
            result.Errors.Select(error => error.Description).Distinct().ToArray());
}
