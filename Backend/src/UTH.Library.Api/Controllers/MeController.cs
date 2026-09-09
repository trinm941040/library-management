using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Profile;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(ICurrentProfileService profileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CurrentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Authenticated user identifier is invalid."));
        var profile = await profileService.GetAsync(userId, Roles(), Permissions(), cancellationToken);
        return profile is null ? Unauthorized(Problem("The current account is unavailable.")) : Ok(Map(profile));
    }

    [HttpPatch("profile")]
    [ProducesResponseType(typeof(CurrentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CurrentProfileResponse>> Update(
        UpdateCurrentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Authenticated user identifier is invalid."));
        if (request.DateOfBirth is not null && request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth cannot be in the future.");
            return UnprocessableEntity(new ValidationProblemDetails(ModelState) { Status = StatusCodes.Status422UnprocessableEntity });
        }
        var result = await profileService.UpdateAsync(
            userId,
            new UpdateCurrentProfileCommand(request.FullName, request.PhoneNumber, request.DateOfBirth, request.Address, request.RowVersion, HttpContext.TraceIdentifier),
            Roles(), Permissions(), cancellationToken);
        return result.Failure switch
        {
            CurrentProfileFailure.None when result.Profile is not null => Ok(Map(result.Profile)),
            CurrentProfileFailure.Conflict => Conflict(Problem(result.Error!)),
            CurrentProfileFailure.NotFound => NotFound(Problem(result.Error!)),
            _ => UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Profile validation failed", Detail = result.Error })
        };
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(Problem("Authenticated user identifier is invalid."));
        var result = await profileService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier, cancellationToken);
        if (!result.Succeeded)
            return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Password change failed", Detail = result.Error });
        Response.Cookies.Delete("uth_refresh", new CookieOptions { Path = "/api/v1/auth" });
        return NoContent();
    }

    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
    private string[] Roles() => User.Claims.Where(value => value.Type is ClaimTypes.Role or "role").Select(value => value.Value).Distinct().ToArray();
    private string[] Permissions() => User.FindAll("permission").Select(value => value.Value).Distinct().ToArray();
    private static ProblemDetails Problem(string detail) => new() { Detail = detail };
    private static CurrentProfileResponse Map(CurrentProfile value) => new(
        value.UserId, value.EmployeeId, value.DisplayName, value.LoginIdentifier, value.LastLoginAtUtc,
        value.EmployeeCode, value.FullName, value.PhoneNumber, value.DateOfBirth, value.Address,
        value.Position, value.Department, value.EmploymentStatus,
        value.Branch is null ? null : new BranchResponse(value.Branch.Id, value.Branch.Code, value.Branch.Name),
        value.Roles, value.Permissions, value.RowVersion);
}
