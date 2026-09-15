using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    LibraryDbContext db,
    IJwtTokenService jwtTokenService,
    IAuthorizationStateService authorizationStateService,
    ICurrentProfileService currentProfileService,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAuthService
{
    private const string InvalidLogin = "Thông tin đăng nhập không hợp lệ.";
    private const string InvalidSession = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.";
    private readonly JwtOptions settings = options.Value;

    public async Task<(bool Succeeded, string? Error, AuthResult? Result)> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        var signIn = user is null
            ? SignInResult.Failed
            : await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signIn.Succeeded || user is null ||
            (settings.RequireConfirmedEmail && !user.EmailConfirmed))
            return await LoginFailedAsync(correlationId, cancellationToken);

        var authorization = await authorizationStateService.GetAsync(user.Id, cancellationToken);
        if (authorization is null || !authorization.CanAuthenticate)
            return await LoginFailedAsync(correlationId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, user.Id, cancellationToken);
        user.LastLoginAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) return Failed(InvalidLogin);

        var result = await IssueNewFamilyAsync(
            user.Id,
            authorization,
            ipAddress,
            userAgent,
            correlationId,
            cancellationToken);
        if (result.Succeeded) await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<(bool Succeeded, string? Error, AuthResult? Result)> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.RefreshTokenSessions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.TokenHash == tokenHash, cancellationToken);
        if (session is null) return Failed(InvalidSession);
        await SessionLock.AcquireAsync(db, session.UserId, cancellationToken);
        session = await db.RefreshTokenSessions.AsNoTracking().SingleAsync(value => value.Id == session.Id, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (session.UsedAtUtc is not null || session.RevokedAtUtc is not null)
        {
            await RevokeFamilyForReuseAsync(session, now, ipAddress, correlationId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Failed(InvalidSession);
        }

        if (session.ExpiresAtUtc <= now) return Failed(InvalidSession);

        var authorization = await authorizationStateService.GetAsync(session.UserId, cancellationToken);
        if (authorization is null || !authorization.CanAuthenticate)
        {
            await RevokeFamilyAsync(session.FamilyId, now, ipAddress, "account-unavailable", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Failed(InvalidSession);
        }

        var replacementId = Guid.NewGuid();
        var affected = await db.RefreshTokenSessions
            .Where(value => value.Id == session.Id &&
                value.UsedAtUtc == null &&
                value.RevokedAtUtc == null &&
                value.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(value => value.UsedAtUtc, now)
                    .SetProperty(value => value.RevokedAtUtc, now)
                    .SetProperty(value => value.RevokedByIp, ipAddress)
                    .SetProperty(value => value.RevocationReason, "rotated")
                    .SetProperty(value => value.ReplacedByTokenId, replacementId),
                cancellationToken);
        if (affected != 1)
        {
            await RevokeFamilyForReuseAsync(session, now, ipAddress, correlationId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Failed(InvalidSession);
        }

        var rawRefreshToken = CreateRefreshToken();
        var replacement = CreateSession(
            replacementId,
            session.UserId,
            rawRefreshToken,
            session.FamilyId,
            session.Id,
            now,
            session.ExpiresAtUtc,
            ipAddress,
            userAgent);
        db.RefreshTokenSessions.Add(replacement);
        db.AuditLogs.Add(AuditLog.Create(
            session.UserId,
            "session.refreshed",
            nameof(RefreshTokenSession),
            replacement.Id,
            null,
            null,
            now,
            correlationId));

        var result = await CreateResultAsync(
            authorization,
            replacement,
            rawRefreshToken,
            cancellationToken);
        if (!result.Succeeded) return result;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> LogoutAsync(
        string refreshToken,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.RefreshTokenSessions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.TokenHash == Hash(refreshToken), cancellationToken);
        if (session is null) return false;
        await SessionLock.AcquireAsync(db, session.UserId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await RevokeFamilyAsync(session.FamilyId, now, ipAddress, "logout", cancellationToken);
        db.AuditLogs.Add(AuditLog.Create(
            session.UserId,
            "session.logged-out",
            nameof(RefreshTokenSession),
            session.Id,
            null,
            null,
            now,
            correlationId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task LogoutAllAsync(
        Guid userId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SessionLock.AcquireAsync(db, userId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await db.RefreshTokenSessions
            .Where(value => value.UserId == userId && value.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(value => value.RevokedAtUtc, now)
                    .SetProperty(value => value.RevokedByIp, ipAddress)
                    .SetProperty(value => value.RevocationReason, "logout-all"),
                cancellationToken);
        db.AuditLogs.Add(AuditLog.Create(
            userId,
            "session.logged-out-all",
            nameof(RefreshTokenSession),
            userId,
            null,
            null,
            now,
            correlationId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<CurrentProfile?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        currentProfileService.GetAsync(userId, cancellationToken);

    private async Task<(bool Succeeded, string? Error, AuthResult? Result)> IssueNewFamilyAsync(
        Guid userId,
        EffectiveAuthorizationState authorization,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rawRefreshToken = CreateRefreshToken();
        var session = CreateSession(
            Guid.NewGuid(),
            userId,
            rawRefreshToken,
            Guid.NewGuid(),
            null,
            now,
            now.AddDays(settings.RefreshTokenDays),
            ipAddress,
            userAgent);
        db.RefreshTokenSessions.Add(session);
        db.AuditLogs.Add(AuditLog.Create(
            userId,
            "session.logged-in",
            nameof(RefreshTokenSession),
            session.Id,
            null,
            null,
            now,
            correlationId));

        var result = await CreateResultAsync(
            authorization,
            session,
            rawRefreshToken,
            cancellationToken);
        if (!result.Succeeded) return result;

        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<(bool Succeeded, string? Error, AuthResult? Result)> CreateResultAsync(
        EffectiveAuthorizationState authorization,
        RefreshTokenSession session,
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentProfileService.GetAsync(authorization.UserId, cancellationToken);
        if (currentUser is null) return Failed(InvalidSession);

        var access = jwtTokenService.CreateAccessToken(
            authorization.UserId,
            authorization.Email,
            currentUser.Roles,
            currentUser.Permissions,
            session.FamilyId);
        return (
            true,
            null,
            new AuthResult(
                authorization.UserId,
                access.Token,
                rawRefreshToken,
                access.ExpiresAtUtc,
                session.ExpiresAtUtc,
                currentUser));
    }

    private async Task RevokeFamilyForReuseAsync(
        RefreshTokenSession session,
        DateTime now,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        await RevokeFamilyAsync(
            session.FamilyId,
            now,
            ipAddress,
            "refresh-reuse",
            cancellationToken);
        db.AuditLogs.Add(AuditLog.Create(
            session.UserId,
            "session.refresh-reuse-detected",
            nameof(RefreshTokenSession),
            session.Id,
            null,
            null,
            now,
            correlationId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTime now,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken) =>
        db.RefreshTokenSessions
            .Where(value => value.FamilyId == familyId && value.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(value => value.RevokedAtUtc, now)
                    .SetProperty(value => value.RevokedByIp, ipAddress)
                    .SetProperty(value => value.RevocationReason, reason),
                cancellationToken);

    private static RefreshTokenSession CreateSession(
        Guid id,
        Guid userId,
        string rawRefreshToken,
        Guid familyId,
        Guid? parentTokenId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? ipAddress,
        string? userAgent) =>
        new()
        {
            Id = id,
            UserId = userId,
            TokenHash = Hash(rawRefreshToken),
            FamilyId = familyId,
            ParentTokenId = parentTokenId,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

    private static string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private async Task<(bool Succeeded, string? Error, AuthResult? Result)> LoginFailedAsync(
        string? correlationId, CancellationToken cancellationToken)
    {
        // No submitted identifier, password or token is persisted in failed-login audit.
        db.AuditLogs.Add(AuditLog.Create(null, "session.login-failed", nameof(ApplicationUser),
            Guid.Empty, null, null, timeProvider.GetUtcNow().UtcDateTime, correlationId));
        await db.SaveChangesAsync(cancellationToken);
        return Failed(InvalidLogin);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static (bool Succeeded, string? Error, AuthResult? Result) Failed(string error) =>
        (false, error, null);
}
