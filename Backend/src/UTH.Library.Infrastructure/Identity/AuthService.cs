using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    LibraryDbContext db,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAuthService
{
    private readonly JwtOptions settings = options.Value;

    public async Task<(bool Succeeded, string? Error, AuthResult? Result)> RegisterAsync(string email, string password, string displayName, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        email = email.Trim().ToUpperInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            return (false, "Registration could not be completed.", null);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = displayName.Trim(), CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime };
        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
            return (false, "Registration could not be completed.", null);
        await userManager.AddToRoleAsync(user, RoleNames.User);
        return await IssueAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error, AuthResult? Result)> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        var result = user is null ? SignInResult.Failed : await signInManager.CheckPasswordSignInAsync(user, password, true);
        if (!result.Succeeded || user is null || !user.IsActive || (settings.RequireConfirmedEmail && !user.EmailConfirmed))
            return (false, "Invalid credentials.", null);
        user.LastLoginAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await userManager.UpdateAsync(user);
        return await IssueAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error, AuthResult? Result)> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.RefreshTokenSessions.Include(value => value.User).SingleOrDefaultAsync(value => value.TokenHash == hash, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (session is null || session.ExpiresAtUtc <= now || session.RevokedAtUtc is not null || session.UsedAtUtc is not null)
        {
            if (session is not null)
            {
                await db.RefreshTokenSessions.Where(value => value.FamilyId == session.FamilyId && value.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters.SetProperty(value => value.RevokedAtUtc, now).SetProperty(value => value.RevocationReason, "refresh-reuse"), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            return (false, "Invalid refresh token.", null);
        }
        session.UsedAtUtc = now;
        session.RevokedAtUtc = now;
        session.RevocationReason = "rotated";
        var result = await IssueAsync(session.User, ipAddress, userAgent, cancellationToken, session.FamilyId, session.Id);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        var session = await db.RefreshTokenSessions.SingleOrDefaultAsync(value => value.TokenHash == Hash(refreshToken), cancellationToken);
        if (session is null) return false;
        session.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        session.RevokedByIp = ipAddress;
        session.RevocationReason = "logout";
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task LogoutAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken) =>
        await db.RefreshTokenSessions.Where(value => value.UserId == userId && value.RevokedAtUtc == null).ExecuteUpdateAsync(setters => setters.SetProperty(value => value.RevokedAtUtc, timeProvider.GetUtcNow().UtcDateTime).SetProperty(value => value.RevokedByIp, ipAddress).SetProperty(value => value.RevocationReason, "logout-all"), cancellationToken);

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new UserProfile(user.Id, user.Email ?? string.Empty, user.DisplayName, (await userManager.GetRolesAsync(user)).ToArray());
    }

    private async Task<(bool Succeeded, string? Error, AuthResult? Result)> IssueAsync(ApplicationUser user, string? ipAddress, string? userAgent, CancellationToken cancellationToken, Guid? familyId = null, Guid? parentId = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await db.RolePermissions.Where(value => roles.Contains(value.Role.Name!)).Select(value => value.Permission.Name).Distinct().ToListAsync(cancellationToken);
        var access = jwtTokenService.CreateAccessToken(user.Id, user.Email ?? string.Empty, roles, permissions);
        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var session = new RefreshTokenSession { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = Hash(rawRefresh), FamilyId = familyId ?? Guid.NewGuid(), ParentTokenId = parentId, CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(settings.RefreshTokenDays), CreatedByIp = ipAddress, UserAgent = userAgent };
        db.RefreshTokenSessions.Add(session);
        return (true, null, new AuthResult(user.Id, access.Token, rawRefresh, access.ExpiresAtUtc, session.ExpiresAtUtc));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
