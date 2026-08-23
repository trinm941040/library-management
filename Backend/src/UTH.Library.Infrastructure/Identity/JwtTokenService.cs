using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions settings = options.Value;

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        try
        {
            // if (string.IsNullOrWhiteSpace(settings.PrivateKeyPem))
            //     throw new InvalidOperationException("JWT signing key is not configured.");
            //
            // using var rsa = RSA.Create();
            // rsa.ImportFromPem(settings.PrivateKeyPem);
            var now = timeProvider.GetUtcNow();
            var expires = now.AddMinutes(settings.AccessTokenMinutes);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, email)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                SecurityAlgorithms.HmacSha256Signature);
            var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now.UtcDateTime,
                expires.UtcDateTime, credentials);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return (accessToken, expires);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
