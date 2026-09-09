using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Auth;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookie = "uth_refresh";

    /// <summary>
    /// Registers a new user with the provided email, password, and display name. If successful, returns an access token and sets a refresh token in an HTTP-only cookie.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) => Issue(await authService.RegisterAsync(request.Email, request.Password, request.DisplayName, Ip(), UserAgent(), cancellationToken));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) => Issue(await authService.LoginAsync(request.Email, request.Password, Ip(), UserAgent(), cancellationToken));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var refreshToken)) return Unauthorized();
        return Issue(await authService.RefreshAsync(refreshToken, Ip(), UserAgent(), cancellationToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookie, out var token)) await authService.LogoutAsync(token, Ip(), HttpContext.TraceIdentifier, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (UserId() is Guid userId) await authService.LogoutAllAsync(userId, Ip(), cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var profile = UserId() is Guid userId ? await authService.GetProfileAsync(userId, cancellationToken) : null;
        return profile is null ? Unauthorized() : Ok(new ProfileResponse(profile.Id, profile.Email, profile.DisplayName, profile.Roles));
    }

    private IActionResult Issue((bool Succeeded, string? Error, AuthResult? Result) result)
    {
        if (!result.Succeeded || result.Result is null)
            return Unauthorized(new ProblemDetails { Title = result.Error ?? "Authentication failed." });
        Response.Cookies.Append(RefreshCookie, result.Result.RefreshToken, new CookieOptions { HttpOnly = true, Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(), SameSite = SameSiteMode.Strict, Path = "/api/v1/auth", Expires = result.Result.RefreshTokenExpiresAtUtc });
        return Ok(new TokenResponse(result.Result.AccessToken, result.Result.AccessTokenExpiresAtUtc));
    }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? UserAgent() => Request.Headers.UserAgent.ToString();
    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/v1/auth" });
}
