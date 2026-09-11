using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class AuthorizationStateService(
    LibraryDbContext db,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAuthorizationStateService
{
    public async Task<EffectiveAuthorizationState?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(value => value.Id == userId)
            .Select(value => new
            {
                value.Id,
                Email = value.Email ?? value.UserName ?? string.Empty,
                value.IsActive,
                value.EmailConfirmed,
                value.LockoutEnd
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null) return null;

        var now = timeProvider.GetUtcNow();
        var accountIsActive = user.IsActive && (!options.Value.RequireConfirmedEmail || user.EmailConfirmed) &&
            (user.LockoutEnd is null || user.LockoutEnd <= now);
        var employeeIsActive = await db.Employees.AsNoTracking().AnyAsync(
            employee => employee.UserId == userId && employee.Status == EmploymentStatus.Active,
            cancellationToken);

        var roles = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.IsActive
            orderby role.Name
            select new { role.Id, Name = role.Name! })
            .ToArrayAsync(cancellationToken);
        var roleIds = roles.Select(role => role.Id).ToArray();
        var permissions = roleIds.Length == 0
            ? []
            : await db.RolePermissions.AsNoTracking()
                .Where(assignment => roleIds.Contains(assignment.RoleId))
                .Select(assignment => assignment.Permission.Name)
                .Distinct()
                .OrderBy(permission => permission)
                .ToArrayAsync(cancellationToken);

        return new EffectiveAuthorizationState(
            user.Id,
            user.Email,
            accountIsActive,
            employeeIsActive,
            roles.Select(role => role.Name).Distinct().ToArray(),
            permissions);
    }

    public Task<bool> IsSessionActiveAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return db.RefreshTokenSessions.AsNoTracking().AnyAsync(session =>
            session.UserId == userId && session.FamilyId == familyId &&
            session.RevokedAtUtc == null && session.UsedAtUtc == null && session.ExpiresAtUtc > now,
            cancellationToken);
    }
}
