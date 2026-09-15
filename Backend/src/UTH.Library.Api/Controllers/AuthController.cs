using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Auth;
using UTH.Library.Api.Contracts.Profile;
using UTH.Library.Application.Abstractions.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookie = "uth_refresh";

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsBrowserRequest()) return BrowserRequestRejected();
        return Issue(await authService.LoginAsync(request.Email, request.Password, Ip(), UserAgent(), HttpContext.TraceIdentifier, cancellationToken));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!IsBrowserRequest()) return BrowserRequestRejected();
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var refreshToken))
            return AuthenticationFailed("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        return Issue(await authService.RefreshAsync(refreshToken, Ip(), UserAgent(), HttpContext.TraceIdentifier, cancellationToken));
    }

    [HttpPost("logout")]
    [HttpPost("revoke")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!IsBrowserRequest()) return BrowserRequestRejected();
        if (Request.Cookies.TryGetValue(RefreshCookie, out var token)) await authService.LogoutAsync(token, Ip(), HttpContext.TraceIdentifier, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (!IsBrowserRequest()) return BrowserRequestRejected();
        if (UserId() is Guid userId)
            await authService.LogoutAllAsync(userId, Ip(), HttpContext.TraceIdentifier, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [HttpGet("current-session")]
    public async Task<ActionResult<CurrentProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var profile = UserId() is Guid userId ? await authService.GetProfileAsync(userId, cancellationToken) : null;
        return profile is null
            ? AuthenticationFailed("Tài khoản hiện tại không khả dụng.")
            : Ok(CurrentProfileResponseMapper.Map(profile));
    }

    private IActionResult Issue((bool Succeeded, string? Error, AuthResult? Result) result)
    {
        if (!result.Succeeded || result.Result is null)
            return AuthenticationFailed(result.Error ?? "Không thể xác thực.");
        Response.Cookies.Append(RefreshCookie, result.Result.RefreshToken, new CookieOptions { HttpOnly = true, Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(), SameSite = SameSiteMode.Strict, Path = "/api/v1/auth", Expires = result.Result.RefreshTokenExpiresAtUtc });
        return Ok(new SessionResponse(
            result.Result.AccessToken,
            result.Result.AccessTokenExpiresAtUtc,
            CurrentProfileResponseMapper.Map(result.Result.CurrentUser)));
    }

    private UnauthorizedObjectResult AuthenticationFailed(string detail)
    {
        DeleteRefreshCookie();
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Không thể xác thực",
            Detail = detail,
            Extensions = { ["code"] = "authentication.failed", ["correlationId"] = HttpContext.TraceIdentifier }
        });
    }

    private ObjectResult BrowserRequestRejected() => Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Yêu cầu không được phép",
        detail: "Yêu cầu xác thực không hợp lệ.");

    // A custom header cannot be sent by cross-origin HTML forms. No credentialed CORS
    // is enabled; browser requests use the same-origin Vite/reverse proxy.
    private bool IsBrowserRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest" &&
        Request.Headers["Sec-Fetch-Site"] != "cross-site";

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? UserAgent()
    {
        var value = Request.Headers.UserAgent.ToString();
        return value.Length > 500 ? value[..500] : value;
    }
    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/v1/auth" });
}
