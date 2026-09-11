using UTH.Library.Api.Contracts.Profile;
using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(1024)] string Password);
public sealed record SessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    CurrentProfileResponse CurrentUser);
