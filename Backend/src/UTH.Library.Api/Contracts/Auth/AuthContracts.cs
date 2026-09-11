namespace UTH.Library.Api.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc);
public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);