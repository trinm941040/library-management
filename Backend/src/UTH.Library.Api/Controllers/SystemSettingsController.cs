using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Settings;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Settings;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.SettingsRead)]
[Route("api/v1/settings")]
public sealed class SystemSettingsController(ISystemConfigurationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SystemSettingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemSettingResponse>>> Get(CancellationToken cancellationToken) =>
        Ok((await service.GetSettingsAsync(cancellationToken)).Select(Map).ToArray());

    [HttpPut("{key}")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    [ProducesResponseType(typeof(SystemSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SystemSettingResponse>> Update(
        string key,
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var setting = await service.UpdateSettingAsync(
            key,
            new UpdateSystemSettingCommand(request.Value, request.ConcurrencyToken),
            UserId(),
            cancellationToken);
        return Ok(Map(setting));
    }

    private Guid UserId() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedAccessException("Không xác định được tài khoản hiện tại.");

    private static SystemSettingResponse Map(SystemSettingModel value) => new(
        value.Key, value.DisplayName, value.Description, value.ValueType, value.Scope,
        value.IsSecret, value.HasValue, value.Value, value.DefaultValue,
        value.UpdatedByUserId, value.UpdatedAtUtc, value.ConcurrencyToken);
}
