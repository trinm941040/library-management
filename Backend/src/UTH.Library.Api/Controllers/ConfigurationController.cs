using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Settings;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Settings;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Route("api/v1/configuration")]
public sealed class ConfigurationController(ISystemConfigurationService service) : ControllerBase
{
    private const int MaximumPackageBytes = 1_048_576;

    [HttpPost("export")]
    [Authorize(Policy = Permissions.SettingsRead)]
    [Produces("application/json")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var package = await service.ExportAsync(UserId(), cancellationToken);
        Response.Headers["X-Configuration-Checksum"] = package.Checksum;
        Response.Headers["X-Configuration-Schema-Version"] = package.SchemaVersion;
        return File(Encoding.UTF8.GetBytes(package.Content), "application/json", package.FileName);
    }

    [HttpPost("import/validate")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumPackageBytes + 64 * 1024)]
    [ProducesResponseType(typeof(ConfigurationImportPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationImportPreviewResponse>> ValidateImport(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is 0 or > MaximumPackageBytes)
            return Problem(statusCode: 422, title: "Tệp không hợp lệ", detail: "Tệp cấu hình vượt giới hạn 1 MiB hoặc trống.");
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var preview = await service.ValidateImportAsync(file.FileName, stream.ToArray(), cancellationToken);
        return Ok(new ConfigurationImportPreviewResponse(
            preview.SchemaVersion,
            preview.Checksum,
            preview.ExportedAtUtc,
            preview.ExpiresAtUtc,
            preview.Differences.Select(value => new ConfigurationDifferenceResponse(
                value.Key, value.DisplayName, value.Scope, value.ValueType,
                value.Kind.ToString(), value.Before, value.After)).ToArray(),
            preview.ConfirmationToken));
    }

    [HttpPost("import/confirm")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    [ProducesResponseType(typeof(ConfigurationImportResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfigurationImportResultResponse>> ConfirmImport(
        ConfirmConfigurationImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmImportAsync(request.ConfirmationToken, UserId(), cancellationToken);
        return Ok(new ConfigurationImportResultResponse(
            result.PackageId, result.Checksum, result.Added, result.Changed,
            result.Removed, result.AppliedAtUtc));
    }

    private Guid UserId() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedAccessException("Không xác định được tài khoản hiện tại.");
}
