using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class AccessAccountService(
    UserManager<ApplicationUser> userManager,
    LibraryDbContext db,
    TimeProvider timeProvider,
    IRequestContext requestContext) : IAccessAccountService
{
    public async Task<AccessAccountPage> GetAsync(
        AccessAccountListQuery query,
        CancellationToken cancellationToken)
    {
        var accounts = db.Users.AsNoTracking()
            .Where(user => db.Employees.Any(employee => employee.UserId == user.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            accounts = accounts.Where(user =>
                (user.NormalizedEmail != null && user.NormalizedEmail.Contains(search)) ||
                user.DisplayName.ToUpper().Contains(search) ||
                db.Employees.Any(employee => employee.UserId == user.Id &&
                    (employee.EmployeeCode.ToUpper().Contains(search) ||
                     employee.FullName.ToUpper().Contains(search))));
        }
        if (query.IsActive is bool active)
            accounts = accounts.Where(user => user.IsActive == active);

        var totalCount = await accounts.CountAsync(cancellationToken);
        var users = await accounts.OrderByDescending(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        return new AccessAccountPage(
            await MapAsync(users, cancellationToken),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<AccessAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (user is null || !await db.Employees.AsNoTracking().AnyAsync(value => value.UserId == id, cancellationToken))
            return null;
        return (await MapAsync([user], cancellationToken)).Single();
    }

    public async Task<IReadOnlyCollection<EligibleAccessAccountEmployee>> GetEligibleEmployeesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Employees.AsNoTracking()
            .Where(employee => employee.UserId == null && employee.Status == EmploymentStatus.Active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();
            query = query.Where(employee =>
                employee.EmployeeCode.ToUpper().Contains(value) ||
                employee.FullName.ToUpper().Contains(value) ||
                employee.Email.ToUpper().Contains(value));
        }
        return await query.OrderBy(employee => employee.FullName)
            .Take(100)
            .Select(employee => new EligibleAccessAccountEmployee(
                employee.Id, employee.EmployeeCode, employee.FullName, employee.Email))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AccessAccountResult> CreateAsync(
        CreateAccessAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.DisplayName))
            return Failed(AccessAccountFailure.Validation, "Email và tên hiển thị là bắt buộc.");
        var roleIds = command.RoleIds.Distinct().ToArray();
        if (roleIds.Length == 0)
            return Failed(AccessAccountFailure.Validation, "Tài khoản phải có ít nhất một vai trò.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, command.EmployeeId, cancellationToken);
        var employee = await db.Employees.SingleOrDefaultAsync(value => value.Id == command.EmployeeId, cancellationToken);
        if (employee is null)
            return Failed(AccessAccountFailure.NotFound, "Không tìm thấy nhân viên.");
        if (employee.UserId is not null)
            return Failed(AccessAccountFailure.Conflict, "Nhân viên đã có tài khoản truy cập.");
        if (employee.Status != EmploymentStatus.Active)
            return Failed(AccessAccountFailure.Conflict, "Chỉ có thể cấp tài khoản cho nhân viên đang làm việc.");

        var email = command.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Failed(AccessAccountFailure.Conflict, "Email đã được sử dụng bởi tài khoản khác.");
        var roles = await db.Roles.Where(role => roleIds.Contains(role.Id) && role.IsActive).ToArrayAsync(cancellationToken);
        if (roles.Length != roleIds.Length)
            return Failed(AccessAccountFailure.Validation, "Một hoặc nhiều vai trò không tồn tại hoặc đã ngừng hoạt động.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = command.DisplayName.Trim(),
            IsActive = true,
            CreatedAtUtc = now
        };
        var created = await userManager.CreateAsync(user, command.Password);
        if (!created.Succeeded) return IdentityFailure(created);
        var assigned = await userManager.AddToRolesAsync(user, roles.Select(role => role.Name!).ToArray());
        if (!assigned.Succeeded) return IdentityFailure(assigned);

        employee.LinkUser(user.Id, now);
        AddAudit(command.ActorUserId, "access-account.created", user.Id, null, new
        {
            user.Id,
            EmployeeId = employee.Id,
            RoleIds = roleIds,
            user.IsActive
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccessAccountResult.Success((await MapAsync([user], cancellationToken)).Single());
    }

    public async Task<AccessAccountResult> SetStatusAsync(
        Guid id,
        bool isActive,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (!isActive && id == actorUserId)
            return Failed(AccessAccountFailure.ProtectedResource, "Không thể khóa tài khoản đang thực hiện thao tác.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, id, cancellationToken);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null || !await db.Employees.AnyAsync(value => value.UserId == id, cancellationToken))
            return Failed(AccessAccountFailure.NotFound, "Không tìm thấy tài khoản truy cập.");
        if (!isActive && await userManager.IsInRoleAsync(user, RoleNames.Administrator))
            return Failed(AccessAccountFailure.ProtectedResource, "Không thể khóa tài khoản quản trị hệ thống.");
        if (user.IsActive == isActive)
            return AccessAccountResult.Success(await GetByIdAsync(id, cancellationToken));

        var before = new { user.IsActive };
        user.IsActive = isActive;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded) return IdentityFailure(updated);
        if (!isActive)
        {
            var stamp = await userManager.UpdateSecurityStampAsync(user);
            if (!stamp.Succeeded) return IdentityFailure(stamp);
            await RevokeAllSessionsAsync(id, "account-locked", cancellationToken);
        }
        AddAudit(actorUserId, isActive ? "access-account.unlocked" : "access-account.locked", id,
            before, new { user.IsActive });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccessAccountResult.Success(await GetByIdAsync(id, cancellationToken));
    }

    public async Task<AccessAccountResult> ResetPasswordAsync(
        Guid id,
        string temporaryPassword,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, id, cancellationToken);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null || !await db.Employees.AnyAsync(value => value.UserId == id, cancellationToken))
            return Failed(AccessAccountFailure.NotFound, "Không tìm thấy tài khoản truy cập.");
        if (id != actorUserId && await userManager.IsInRoleAsync(user, RoleNames.Administrator))
            return Failed(AccessAccountFailure.ProtectedResource, "Không thể đặt lại mật khẩu của quản trị viên khác.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (!reset.Succeeded) return IdentityFailure(reset);
        await RevokeAllSessionsAsync(id, "password-reset-by-administrator", cancellationToken);
        AddAudit(actorUserId, "access-account.password-reset", id,
            new { Password = "[REDACTED]" }, new { Password = "[REDACTED]" });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccessAccountResult.Success();
    }

    public async Task<IReadOnlyCollection<AccessAccountSession>?> GetSessionsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await db.Employees.AsNoTracking().AnyAsync(value => value.UserId == id, cancellationToken)) return null;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await db.RefreshTokenSessions.AsNoTracking()
            .Where(session => session.UserId == id)
            .OrderByDescending(session => session.CreatedAtUtc)
            .Take(100)
            .Select(session => new AccessAccountSession(
                session.Id,
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.UsedAtUtc,
                session.RevokedAtUtc,
                session.CreatedByIp,
                session.UserAgent,
                session.RevocationReason,
                session.RevokedAtUtc == null && session.UsedAtUtc == null && session.ExpiresAtUtc > now))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AccessAccountResult> RevokeSessionAsync(
        Guid id,
        Guid sessionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, id, cancellationToken);
        var session = await db.RefreshTokenSessions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == sessionId && value.UserId == id, cancellationToken);
        if (session is null)
            return Failed(AccessAccountFailure.NotFound, "Không tìm thấy phiên đăng nhập.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var affected = await db.RefreshTokenSessions
            .Where(value => value.FamilyId == session.FamilyId && value.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.RevokedAtUtc, now)
                .SetProperty(value => value.RevocationReason, "revoked-by-administrator"), cancellationToken);
        AddAudit(actorUserId, "access-account.session-revoked", sessionId, null,
            new { AccountId = id, SessionId = sessionId, RevokedSessions = affected });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccessAccountResult.Success();
    }

    private async Task<IReadOnlyCollection<AccessAccount>> MapAsync(
        IReadOnlyCollection<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        if (users.Count == 0) return [];
        var ids = users.Select(user => user.Id).ToArray();
        var employees = await db.Employees.AsNoTracking().Where(employee =>
                employee.UserId != null && ids.Contains(employee.UserId.Value))
            .ToDictionaryAsync(employee => employee.UserId!.Value, cancellationToken);
        var roleRows = await (from assignment in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where ids.Contains(assignment.UserId) && role.IsActive
            select new { assignment.UserId, Role = role.Name! }).ToArrayAsync(cancellationToken);
        var roles = roleRows.GroupBy(value => value.UserId).ToDictionary(
            group => group.Key,
            group => (IReadOnlyCollection<string>)group.Select(value => value.Role).Distinct().Order().ToArray());
        return users.Where(user => employees.ContainsKey(user.Id)).Select(user =>
        {
            var employee = employees[user.Id];
            var assignedRoles = roles.GetValueOrDefault(user.Id, []);
            return new AccessAccount(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.IsActive,
                user.EmailConfirmed,
                user.CreatedAtUtc,
                user.LastLoginAtUtc,
                assignedRoles,
                new AccessAccountEmployee(employee.Id, employee.EmployeeCode, employee.FullName,
                    employee.Email, employee.Status.ToString()),
                assignedRoles.Contains(RoleNames.Administrator, StringComparer.OrdinalIgnoreCase));
        }).ToArray();
    }

    private async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await db.RefreshTokenSessions.Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevocationReason, reason), cancellationToken);
    }

    private void AddAudit(Guid actorUserId, string action, Guid entityId, object? before, object? after) =>
        db.AuditLogs.Add(AuditLog.Create(actorUserId, action, nameof(ApplicationUser), entityId,
            before is null ? null : JsonSerializer.Serialize(before),
            after is null ? null : JsonSerializer.Serialize(after),
            timeProvider.GetUtcNow().UtcDateTime,
            requestContext.CorrelationId));

    private static AccessAccountResult IdentityFailure(IdentityResult result) =>
        Failed(result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName")
                ? AccessAccountFailure.Conflict
                : AccessAccountFailure.Validation,
            result.Errors.Select(error => error.Description).Distinct().ToArray());

    private static AccessAccountResult Failed(AccessAccountFailure failure, params string[] errors) =>
        AccessAccountResult.Failed(failure, errors);
}
