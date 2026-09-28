using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Profile;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MeController(ICurrentProfileService profileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CurrentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Mã tài khoản đã xác thực không hợp lệ."));
        var profile = await profileService.GetAsync(userId, cancellationToken);
        return profile is null
            ? Unauthorized(Problem("Tài khoản hiện tại không khả dụng."))
            : Ok(CurrentProfileResponseMapper.Map(profile));
    }

    [HttpPatch("profile")]
    [ProducesResponseType(typeof(CurrentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CurrentProfileResponse>> Update(
        UpdateCurrentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Mã tài khoản đã xác thực không hợp lệ."));
        if (request.DateOfBirth is not null && request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Ngày sinh không được ở tương lai.");
            return UnprocessableEntity(new ValidationProblemDetails(ModelState) { Status = StatusCodes.Status422UnprocessableEntity });
        }
        var result = await profileService.UpdateAsync(
            userId,
            new UpdateCurrentProfileCommand(request.FullName, request.PhoneNumber, request.DateOfBirth, request.Address, request.RowVersion, HttpContext.TraceIdentifier),
            cancellationToken);
        return result.Failure switch
        {
            CurrentProfileFailure.None when result.Profile is not null => Ok(CurrentProfileResponseMapper.Map(result.Profile)),
            CurrentProfileFailure.Conflict => Conflict(Problem(result.Error!)),
            CurrentProfileFailure.NotFound => NotFound(Problem(result.Error!)),
            _ => UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Hồ sơ không hợp lệ", Detail = result.Error })
        };
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Mã tài khoản đã xác thực không hợp lệ."));
        var result = await profileService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier, cancellationToken);
        if (!result.Succeeded)
            return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Đổi mật khẩu không thành công", Detail = result.Error });
        Response.Cookies.Delete("uth_refresh", new CookieOptions { Path = "/api/v1/auth" });
        return NoContent();
    }

    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
    private static ProblemDetails Problem(string detail) => new() { Detail = detail };
}
