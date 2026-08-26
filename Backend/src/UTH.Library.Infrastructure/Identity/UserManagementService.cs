using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    LibraryDbContext db,
    TimeProvider timeProvider) : IUserManagementService
{
    private const string DefaultRole = "User";

    public async Task<UserPage> GetAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var usersQuery = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            usersQuery = usersQuery.Where(user =>
                (user.NormalizedEmail != null && user.NormalizedEmail.Contains(search)) ||
                user.DisplayName.ToUpper().Contains(search));
        }

        if (query.IsActive is bool isActive)
            usersQuery = usersQuery.Where(user => user.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = userManager.NormalizeName(query.Role.Trim());
            usersQuery = usersQuery.Where(user => db.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                db.Roles.Any(role => role.Id == userRole.RoleId && role.NormalizedName == normalizedRole)));
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .OrderByDescending(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var roles = await GetRolesAsync(users.Select(user => user.Id).ToArray(), cancellationToken);
        var items = users.Select(user => Map(user, roles.GetValueOrDefault(user.Id, []))).ToArray();

        return new UserPage(items, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ManagedUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (user is null)
            return null;

        var roles = await GetRolesAsync([id], cancellationToken);
        return Map(user, roles.GetValueOrDefault(id, []));
    }

    public async Task<UserManagementResult> CreateAsync(
        CreateManagedUserCommand command,
        CancellationToken cancellationToken)
    {
        var email = command.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return UserManagementResult.Failed(UserManagementFailure.Conflict, "A user with this email already exists.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = command.DisplayName.Trim(),
            IsActive = true,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
            return ToFailure(createResult);

        var roleResult = await userManager.AddToRoleAsync(user, DefaultRole);
        if (!roleResult.Succeeded)
            return ToFailure(roleResult);

        await transaction.CommitAsync(cancellationToken);
        return UserManagementResult.Success(Map(user, [DefaultRole]));
    }

    public async Task<UserManagementResult> UpdateAsync(
        Guid id,
        UpdateManagedUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return UserManagementResult.Failed(UserManagementFailure.NotFound, "User was not found.");

        var email = command.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null && existingUser.Id != id)
            return UserManagementResult.Failed(UserManagementFailure.Conflict, "A user with this email already exists.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        user.Email = email;
        user.UserName = email;
        user.DisplayName = command.DisplayName.Trim();
        if (emailChanged)
            user.EmailConfirmed = false;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return ToFailure(updateResult);

        if (emailChanged)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                return ToFailure(stampResult);

            await RevokeSessionsAsync(user.Id, "user-email-updated", cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        return UserManagementResult.Success(Map(user, roles.ToArray()));
    }

    public async Task<UserManagementResult> DeactivateAsync(
        Guid id,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (id == currentUserId)
            return UserManagementResult.Failed(
                UserManagementFailure.SelfDeactivation,
                "You cannot deactivate your own account.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return UserManagementResult.Failed(UserManagementFailure.NotFound, "User was not found.");

        if (!user.IsActive)
            return UserManagementResult.Success();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.IsActive = false;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return ToFailure(updateResult);

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
            return ToFailure(stampResult);

        await RevokeSessionsAsync(user.Id, "user-deactivated", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return UserManagementResult.Success();
    }

    private async Task<Dictionary<Guid, IReadOnlyCollection<string>>> GetRolesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var memberships = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name! })
            .ToListAsync(cancellationToken);

        return memberships
            .GroupBy(value => value.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(value => value.Role).Order().ToArray());
    }

    private async Task RevokeSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await db.RefreshTokenSessions
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.RevocationReason, reason),
                cancellationToken);
    }

    private static UserManagementResult ToFailure(IdentityResult result)
    {
        var errors = result.Errors.Select(error => error.Description).Distinct().ToArray();
        var isConflict = result.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");

        return UserManagementResult.Failed(
            isConflict ? UserManagementFailure.Conflict : UserManagementFailure.Validation,
            errors);
    }

    private static ManagedUser Map(ApplicationUser user, IReadOnlyCollection<string> roles) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            roles);
}
