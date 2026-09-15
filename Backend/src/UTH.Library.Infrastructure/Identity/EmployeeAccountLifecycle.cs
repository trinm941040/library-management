using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

internal sealed class EmployeeAccountLifecycle(
    UserManager<ApplicationUser> userManager,
    LibraryDbContext db,
    TimeProvider timeProvider,
    IRequestContext requestContext) : IEmployeeAccountLifecycle
{
    public async Task<LinkedAccountDeactivationResult> DeactivateAsync(
        Guid? userId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (userId is null) return LinkedAccountDeactivationResult.NotLinked;
        await SessionLock.AcquireAsync(db, userId.Value, cancellationToken);
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null) return LinkedAccountDeactivationResult.NotLinked;
        if (await userManager.IsInRoleAsync(user, RoleNames.Administrator))
            return LinkedAccountDeactivationResult.Protected;
        if (!user.IsActive) return LinkedAccountDeactivationResult.AlreadyInactive;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        user.IsActive = false;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
            throw new InvalidOperationException(string.Join("; ", updated.Errors.Select(error => error.Description)));
        var stamp = await userManager.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded)
            throw new InvalidOperationException(string.Join("; ", stamp.Errors.Select(error => error.Description)));

        var revoked = await db.RefreshTokenSessions
            .Where(session => session.UserId == user.Id && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevocationReason, "employee-terminated"), cancellationToken);
        db.AuditLogs.Add(AuditLog.Create(
            actorUserId,
            "access-account.locked-after-employee-termination",
            nameof(ApplicationUser),
            user.Id,
            JsonSerializer.Serialize(new { IsActive = true }),
            JsonSerializer.Serialize(new { IsActive = false, RevokedSessions = revoked }),
            now,
            requestContext.CorrelationId));
        return LinkedAccountDeactivationResult.Deactivated;
    }
}
