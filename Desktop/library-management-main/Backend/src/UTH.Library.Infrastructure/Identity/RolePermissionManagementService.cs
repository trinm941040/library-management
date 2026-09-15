using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class RolePermissionManagementService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    LibraryDbContext db,
    TimeProvider timeProvider,
    IRequestContext requestContext) : IRolePermissionManagementService
{
    private static readonly string[] RequiredAdministratorPermissions =
    [
        Permissions.RolesRead,
        Permissions.RolesUpdate,
        Permissions.RolesAssign,
        Permissions.PermissionsRead,
        Permissions.PermissionsUpdate
    ];
    private static readonly Regex PermissionNamePattern = new(
        "^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<ManagedPage<ManagedRole>> GetRolesAsync(
        string? search,
        int pageNumber,
        int pageSize,
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

        var totalCount = await query.CountAsync(cancellationToken);
        var roles = await query
            .OrderBy(role => role.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ManagedPage<ManagedRole>(
            await MapRolesAsync(roles, cancellationToken),
            pageNumber,
            pageSize,
            totalCount);
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
            return RoleFailure(RolePermissionManagementFailure.Conflict, "Tên vai trò đã tồn tại.");

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = command.Description.Trim(),
            IsSystemRole = false,
            IsActive = true,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return RoleFailure(result);

        AddAudit("role.created", nameof(ApplicationRole), role.Id, null, RoleSnapshot(role));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RolePermissionManagementResult<ManagedRole>.Success(MapRole(role, []));
    }

    public async Task<RolePermissionManagementResult<ManagedRole>> UpdateRoleAsync(
        Guid id,
        UpdateManagedRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return RoleFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy vai trò.");

        var name = command.Name.Trim();
        if (role.IsSystemRole && !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
            return RoleFailure(RolePermissionManagementFailure.ProtectedResource, "Không thể đổi tên vai trò hệ thống.");
        if (role.IsSystemRole && command.IsActive == false)
            return RoleFailure(RolePermissionManagementFailure.ProtectedResource, "Vai trò hệ thống không thể bị vô hiệu hóa.");

        var duplicate = await roleManager.FindByNameAsync(name);
        if (duplicate is not null && duplicate.Id != id)
            return RoleFailure(RolePermissionManagementFailure.Conflict, "Tên vai trò đã tồn tại.");

        var before = RoleSnapshot(role);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        role.Name = name;
        role.IsActive = command.IsActive ?? role.IsActive;
        role.Description = command.Description.Trim();
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return RoleFailure(result);

        AddAudit("role.updated", nameof(ApplicationRole), role.Id, before, RoleSnapshot(role));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var mapped = await GetRoleByIdAsync(role.Id, cancellationToken);
        return RolePermissionManagementResult<ManagedRole>.Success(mapped!);
    }

    public async Task<RolePermissionOperationResult> DeleteRoleAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return OperationFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy vai trò.");
        if (role.IsSystemRole)
            return OperationFailure(RolePermissionManagementFailure.ProtectedResource, "Không thể xóa vai trò hệ thống.");
        if (await db.UserRoles.AnyAsync(value => value.RoleId == id, cancellationToken))
            return OperationFailure(RolePermissionManagementFailure.Conflict, "Vai trò đang được gán cho ít nhất một tài khoản.");

        var before = RoleSnapshot(role);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded) return OperationFailure(result);
        AddAudit("role.deleted", nameof(ApplicationRole), role.Id, before, null);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return RolePermissionOperationResult.Success();
    }

    public async Task<RolePermissionManagementResult<ManagedRole>> ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, roleId, cancellationToken);
        var role = await db.Roles.SingleOrDefaultAsync(value => value.Id == roleId, cancellationToken);
        if (role is null)
            return RoleFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy vai trò.");

        var requestedIds = permissionIds.Distinct().ToArray();
        var permissions = await db.Permissions
            .Where(permission => requestedIds.Contains(permission.Id))
            .OrderBy(permission => permission.Name)
            .ToListAsync(cancellationToken);
        if (permissions.Count != requestedIds.Length)
            return RoleFailure(RolePermissionManagementFailure.Validation, "Một hoặc nhiều quyền không tồn tại.");

        var requestedNames = permissions.Select(permission => permission.Name).ToHashSet(StringComparer.Ordinal);
        if (role.IsSystemRole && string.Equals(role.Name, RoleNames.Administrator, StringComparison.OrdinalIgnoreCase) &&
            RequiredAdministratorPermissions.Any(permission => !requestedNames.Contains(permission)))
            return RoleFailure(
                RolePermissionManagementFailure.ProtectedResource,
                "Vai trò quản trị hệ thống phải giữ các quyền quản lý vai trò và quyền hạn bắt buộc.");

        var existing = await db.RolePermissions
            .Where(value => value.RoleId == roleId)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(value => value.PermissionId).ToHashSet();
        var before = JsonSerializer.Serialize(existing.Select(value => value.PermissionId).Order().ToArray());
        var requestedIdSet = requestedIds.ToHashSet();
        db.RolePermissions.RemoveRange(existing.Where(value => !requestedIdSet.Contains(value.PermissionId)));
        db.RolePermissions.AddRange(requestedIds.Where(permissionId => !existingIds.Contains(permissionId)).Select(permissionId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        }));
        AddAudit(
            "role.permissions-replaced",
            nameof(RolePermission),
            roleId,
            before,
            JsonSerializer.Serialize(requestedIds.Order().ToArray()));
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
            return OperationFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy tài khoản.");

        var requestedIds = roleIds.Distinct().ToArray();
        if (requestedIds.Length == 0)
            return OperationFailure(RolePermissionManagementFailure.Validation, "Tài khoản phải có ít nhất một vai trò.");

        var requestedRoles = await db.Roles
            .Where(role => requestedIds.Contains(role.Id) && role.IsActive)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
        if (requestedRoles.Count != requestedIds.Length)
            return OperationFailure(RolePermissionManagementFailure.Validation, "Một hoặc nhiều vai trò không tồn tại hoặc đã ngừng sử dụng.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var currentRoleIds = await db.UserRoles
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.RoleId)
            .ToArrayAsync(cancellationToken);
        var administratorRole = await db.Roles.SingleOrDefaultAsync(
            role => role.NormalizedName == RoleNames.Administrator.ToUpper(), cancellationToken);
        if (administratorRole is not null)
            await SessionLock.AcquireAsync(db, administratorRole.Id, cancellationToken);
        if (administratorRole is not null &&
            currentRoleIds.Contains(administratorRole.Id) &&
            !requestedIds.Contains(administratorRole.Id))
        {
            var otherActiveAdministrators = await db.UserRoles.CountAsync(
                assignment => assignment.RoleId == administratorRole.Id &&
                    assignment.UserId != userId &&
                    db.Users.Any(account => account.Id == assignment.UserId && account.IsActive),
                cancellationToken);
            if (otherActiveAdministrators == 0)
                return OperationFailure(
                    RolePermissionManagementFailure.ProtectedResource,
                    "Không thể gỡ vai trò của quản trị viên hoạt động cuối cùng.");
        }

        var requestedNames = requestedRoles.Select(role => role.Name!).ToArray();
        var currentRoles = await userManager.GetRolesAsync(user);

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

            AddAudit(
                "user.roles-replaced",
                nameof(IdentityUserRole<Guid>),
                userId,
                JsonSerializer.Serialize(currentRoleIds.Order().ToArray()),
                JsonSerializer.Serialize(requestedIds.Order().ToArray()));
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return RolePermissionOperationResult.Success();
    }

    public async Task<ManagedPage<ManagedPermission>> GetPermissionsAsync(
        string? search,
        string? module,
        int pageNumber,
        int pageSize,
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

        var totalCount = await query.CountAsync(cancellationToken);
        var permissions = await query
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ManagedPage<ManagedPermission>(
            permissions.Select(MapPermission).ToArray(),
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionModulesAsync(CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking()
            .Select(permission => permission.Module)
            .Distinct()
            .OrderBy(module => module)
            .ToArrayAsync(cancellationToken);

    public async Task<ManagedPermission?> GetPermissionByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking()
            .Where(permission => permission.Id == id)
            .Select(permission => new ManagedPermission(
                permission.Id,
                permission.Name,
                permission.Description,
                permission.Module,
                permission.CreatedAtUtc,
                Permissions.All.Contains(permission.Name)))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RolePermissionManagementResult<ManagedPermission>> CreatePermissionAsync(
        CreateManagedPermissionCommand command,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizePermission(command.Name, command.Module);
        if (normalized.Error is not null)
            return PermissionFailure(RolePermissionManagementFailure.Validation, normalized.Error);
        if (await db.Permissions.AnyAsync(value => value.Name == normalized.Name, cancellationToken))
            return PermissionFailure(RolePermissionManagementFailure.Conflict, "Tên quyền đã tồn tại.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = normalized.Name,
            Description = command.Description.Trim(),
            Module = normalized.Module,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Permissions.Add(permission);
        AddAudit("permission.created", nameof(Permission), permission.Id, null, PermissionSnapshot(permission));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return RolePermissionManagementResult<ManagedPermission>.Success(MapPermission(permission));
    }

    public async Task<RolePermissionManagementResult<ManagedPermission>> UpdatePermissionAsync(
        Guid id,
        UpdateManagedPermissionCommand command,
        CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (permission is null)
            return PermissionFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy quyền.");

        var normalized = NormalizePermission(command.Name, command.Module);
        if (normalized.Error is not null)
            return PermissionFailure(RolePermissionManagementFailure.Validation, normalized.Error);
        if (Permissions.All.Contains(permission.Name) &&
            (!string.Equals(permission.Name, normalized.Name, StringComparison.Ordinal) ||
             !string.Equals(permission.Module, normalized.Module, StringComparison.Ordinal)))
            return PermissionFailure(RolePermissionManagementFailure.ProtectedResource, "Không thể đổi tên hoặc chuyển phân hệ của quyền hệ thống.");
        if (await db.Permissions.AnyAsync(value => value.Id != id && value.Name == normalized.Name, cancellationToken))
            return PermissionFailure(RolePermissionManagementFailure.Conflict, "Tên quyền đã tồn tại.");

        var before = PermissionSnapshot(permission);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        permission.Name = normalized.Name;
        permission.Module = normalized.Module;
        permission.Description = command.Description.Trim();
        AddAudit("permission.updated", nameof(Permission), permission.Id, before, PermissionSnapshot(permission));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return RolePermissionManagementResult<ManagedPermission>.Success(MapPermission(permission));
    }

    public async Task<RolePermissionOperationResult> DeletePermissionAsync(Guid id, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (permission is null)
            return OperationFailure(RolePermissionManagementFailure.NotFound, "Không tìm thấy quyền.");
        if (Permissions.All.Contains(permission.Name))
            return OperationFailure(RolePermissionManagementFailure.ProtectedResource, "Không thể xóa quyền hệ thống.");

        var before = PermissionSnapshot(permission);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Permissions.Remove(permission);
        AddAudit("permission.deleted", nameof(Permission), permission.Id, before, null);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
            permissions.Select(MapPermission).ToArray(),
            role.IsActive);

    private static ManagedPermission MapPermission(Permission permission) =>
        new(
            permission.Id,
            permission.Name,
            permission.Description,
            permission.Module,
            permission.CreatedAtUtc,
            Permissions.All.Contains(permission.Name));

    private static (string Name, string Module, string? Error) NormalizePermission(string name, string module)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var normalizedModule = module.Trim().ToLowerInvariant();
        if (!PermissionNamePattern.IsMatch(normalizedName))
            return (normalizedName, normalizedModule, "Tên quyền phải theo dạng 'phân-hệ.hành-động' với chữ thường, chữ số hoặc dấu gạch ngang.");
        if (!normalizedName.StartsWith($"{normalizedModule}.", StringComparison.Ordinal))
            return (normalizedName, normalizedModule, "Phân hệ phải trùng với tiền tố trong tên quyền.");
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

    private void AddAudit(
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson) =>
        db.AuditLogs.Add(AuditLog.Create(
            requestContext.UserId,
            action,
            entityType,
            entityId,
            beforeJson,
            afterJson,
            timeProvider.GetUtcNow().UtcDateTime,
            requestContext.CorrelationId));

    private static string RoleSnapshot(ApplicationRole role) => JsonSerializer.Serialize(new
    {
        role.Id,
        role.Name,
        role.Description,
        role.IsSystemRole,
        role.IsActive
    });

    private static string PermissionSnapshot(Permission permission) => JsonSerializer.Serialize(new
    {
        permission.Id,
        permission.Name,
        permission.Description,
        permission.Module
    });
}
