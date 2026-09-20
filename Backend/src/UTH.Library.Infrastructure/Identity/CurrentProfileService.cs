using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class CurrentProfileService(
    LibraryDbContext db,
    UserManager<ApplicationUser> userManager,
    IAuthorizationStateService authorizationStateService,
    TimeProvider timeProvider) : ICurrentProfileService
{
    public async Task<CurrentProfile?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizationStateService.GetAsync(userId, cancellationToken);
        if (authorization is null || !authorization.CanAuthenticate) return null;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId, cancellationToken);
        if (user is null) return null;

        var employee = await db.Employees.AsNoTracking()
            .Include(value => value.Branch)
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        return Map(user, employee, authorization.Roles, authorization.Permissions);
    }

    public async Task<CurrentProfileResult> UpdateAsync(
        Guid userId,
        UpdateCurrentProfileCommand command,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizationStateService.GetAsync(userId, cancellationToken);
        if (authorization is null || !authorization.CanAuthenticate)
            return CurrentProfileResult.Failed(CurrentProfileFailure.NotFound, "Tài khoản nhân viên không khả dụng.");
        var user = await db.Users.SingleOrDefaultAsync(value => value.Id == userId, cancellationToken);
        var employee = await db.Employees.Include(value => value.Branch)
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (user is null || employee is null)
            return CurrentProfileResult.Failed(CurrentProfileFailure.NotFound, "Employee profile was not found.");
        if (employee.ConcurrencyToken != command.RowVersion)
            return CurrentProfileResult.Failed(CurrentProfileFailure.Conflict, "The profile was updated elsewhere. Reload it and try again.");

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            employee.UpdatePersonalProfile(command.FullName, command.PhoneNumber, command.DateOfBirth, command.Address, now);
            user.DisplayName = employee.FullName;
            db.AuditLogs.Add(AuditLog.Create(userId, "profile.updated", nameof(Employee), employee.Id, null, null, now, command.CorrelationId));
            await db.SaveChangesAsync(cancellationToken);
            return CurrentProfileResult.Success(Map(user, employee, authorization.Roles, authorization.Permissions));
        }
        catch (ArgumentException exception)
        {
            return CurrentProfileResult.Failed(CurrentProfileFailure.Validation, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CurrentProfileResult.Failed(CurrentProfileFailure.Conflict, "The profile was updated elsewhere. Reload it and try again.");
        }
    }

    public async Task<CurrentProfileResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return CurrentProfileResult.Failed(CurrentProfileFailure.NotFound, "Account was not found.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, userId, cancellationToken);
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return CurrentProfileResult.Failed(CurrentProfileFailure.InvalidPassword, "Password could not be changed. Check the current password and password requirements.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await db.RefreshTokenSessions
            .Where(value => value.UserId == userId && value.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.RevokedAtUtc, now)
                .SetProperty(value => value.RevokedByIp, ipAddress)
                .SetProperty(value => value.RevocationReason, "password-changed"), cancellationToken);
        db.AuditLogs.Add(AuditLog.Create(userId, "password.changed", nameof(ApplicationUser), userId, null, null, now, correlationId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CurrentProfileResult.Success();
    }

    private static CurrentProfile Map(
        ApplicationUser user,
        Employee? employee,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions) =>
        new(
            user.Id,
            employee?.Id,
            user.DisplayName,
            user.Email ?? user.UserName ?? string.Empty,
            user.LastLoginAtUtc,
            employee?.EmployeeCode,
            employee?.FullName,
            employee?.PhoneNumber,
            employee?.DateOfBirth,
            employee?.Address,
            employee?.Position,
            employee?.Department,
            employee?.Status.ToString(),
            employee?.Branch is null ? null : new CurrentBranch(employee.Branch.Id, employee.Branch.Code, employee.Branch.Name),
            roles,
            permissions,
            employee?.ConcurrencyToken);
}
