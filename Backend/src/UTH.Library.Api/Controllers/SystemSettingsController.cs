using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.SystemSettings;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/settings")]
[Route("api/v1/configuration")]
[Route("api/v1/system/config")]
public sealed class SystemSettingsController(ISystemSettingService settingService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.SettingsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<SystemSettingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemSettingDto>>> GetAll(
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        var settings = await settingService.GetAllAsync(scope, cancellationToken);
        return Ok(settings);
    }

    [HttpGet("{key}")]
    [Authorize(Policy = Permissions.SettingsRead)]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemSettingDto>> GetByKey(
        string key,
        CancellationToken cancellationToken)
    {
        var setting = await settingService.GetByKeyAsync(key, cancellationToken);
        if (setting is null) return NotFound(new { message = $"Không tìm thấy thiết lập '{key}'." });
        return Ok(setting);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemSettingDto>> Update(
        string key,
        [FromBody] UpdateSettingCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(key, command.Key, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Key trên URL không khớp với body." });
        }

        try
        {
            var updated = await settingService.UpdateAsync(command, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("batch")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(IReadOnlyList<SystemSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SystemSettingDto>>> BatchUpdate(
        [FromBody] BatchUpdateSettingsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await settingService.BatchUpdateAsync(command, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export-package")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPackage(CancellationToken cancellationToken)
    {
        var package = await settingService.ExportPackageAsync(cancellationToken);
        var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        var filename = $"Cau_Hinh_He_Thong_v{package.Version}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        return File(bytes, "application/json; charset=utf-8", filename);
    }

    [HttpPost("validate-package")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(PackageValidationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PackageValidationResultDto>> ValidatePackage(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var content = body.GetRawText();
        if (string.IsNullOrWhiteSpace(content) || content == "null")
        {
            return BadRequest(new PackageValidationResultDto(false, "Vui lòng gửi nội dung JSON của gói cấu hình.", "", "", 0, []));
        }

        var result = await settingService.ValidatePackageAsync(content, cancellationToken);
        return Ok(result);
    }

    [HttpPost("import-package")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(ConfirmImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfirmImportResult>> ImportPackage(
        [FromBody] ConfirmImportCommand command,
        CancellationToken cancellationToken)
    {
        var result = await settingService.ImportPackageAsync(command, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("packages")]
    [Authorize(Policy = Permissions.SettingsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ConfigurationPackageSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConfigurationPackageSummaryDto>>> GetPackageHistory(CancellationToken cancellationToken)
    {
        var history = await settingService.GetPackageHistoryAsync(cancellationToken);
        return Ok(history);
    }

    [HttpPost("reset-defaults")]
    [Authorize(Policy = Permissions.SettingsManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ResetDefaults(CancellationToken cancellationToken)
    {
        var count = await settingService.ResetToDefaultsAsync(cancellationToken);
        return Ok(new { message = $"Đã khôi phục {count} thiết lập về giá trị mặc định của hệ thống." });
    }
}
