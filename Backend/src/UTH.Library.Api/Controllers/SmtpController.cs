using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Notifications;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Notifications;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.SettingsRead)]
[Route("api/v1/smtp")]
public sealed class SmtpController(ISmtpAdministrationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SmtpSettingsView), StatusCodes.Status200OK)]
    public async Task<ActionResult<SmtpSettingsView>> Get(CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(cancellationToken));

    [HttpPost("test")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    [ProducesResponseType(typeof(SmtpTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SmtpTestResult>> Test(SmtpTestRequest request, CancellationToken cancellationToken)
    {
        var result = await service.TestAsync(
            new SmtpTestCommand(request.Recipient, request.SendMessage), UserId(), cancellationToken);
        return result.Success
            ? Ok(result)
            : BadRequest(new ProblemDetails { Title = "Kiểm tra SMTP thất bại", Detail = result.Message });
    }

    private Guid UserId() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedAccessException("Không xác định được tài khoản hiện tại.");
}
