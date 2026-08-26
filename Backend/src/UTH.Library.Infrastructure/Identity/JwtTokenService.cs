using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Infrastructure.Identity;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    RsaJwtKeyProvider keyProvider) : IJwtTokenService
{
    private readonly JwtOptions settings = options.Value;
    private readonly SigningCredentials signingCredentials =
        new(keyProvider.SigningKey, SecurityAlgorithms.RsaSha256);

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now.UtcDateTime).ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
