namespace UTH.Library.Application.Abstractions.Identity;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, Guid? familyId = null);
}
