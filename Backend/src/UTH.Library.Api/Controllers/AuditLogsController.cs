using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.AuditLogs;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit-logs")]
[Route("api/v1/audit-log")]
[Route("api/v1/system/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(AuditLogPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPageResponse>> Get(
        [FromQuery] string? search,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? ipAddress,
        [FromQuery] string? correlationId,
        [FromQuery] DateTime? fromDateUtc,
        [FromQuery] DateTime? toDateUtc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogListQuery(
            search,
            actorUserId,
            action,
            entityType,
            entityId,
            ipAddress,
            correlationId,
            fromDateUtc,
            toDateUtc,
            pageNumber,
            pageSize);

        var result = await auditLogService.GetPageAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var log = await auditLogService.GetByIdAsync(id, cancellationToken);
        return log is null
            ? NotFound(new ProblemDetails { Title = "Not Found", Detail = "Bản ghi kiểm toán không tồn tại." })
            : Ok(log);
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    [Authorize(Policy = Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetEntityHistory(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var history = await auditLogService.GetEntityHistoryAsync(entityType, entityId, cancellationToken);
        return Ok(history);
    }

    [HttpGet("export-csv")]
    [Authorize(Policy = Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? search,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? ipAddress,
        [FromQuery] string? correlationId,
        [FromQuery] DateTime? fromDateUtc,
        [FromQuery] DateTime? toDateUtc,
        CancellationToken cancellationToken)
    {
        var query = new AuditLogListQuery(
            search,
            actorUserId,
            action,
            entityType,
            entityId,
            ipAddress,
            correlationId,
            fromDateUtc,
            toDateUtc,
            1,
            5000);

        var bytes = await auditLogService.ExportCsvAsync(query, cancellationToken);
        var filename = $"nhat_ky_kiem_toan_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("retention-policy")]
    [Authorize(Policy = Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(AuditLogRetentionPolicyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogRetentionPolicyDto>> GetRetentionPolicy(CancellationToken cancellationToken)
    {
        var policy = await auditLogService.GetRetentionPolicyAsync(cancellationToken);
        return Ok(policy);
    }

    [HttpPost("cleanup-expired")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> CleanupExpired([FromQuery] int retentionDays = 365, CancellationToken cancellationToken = default)
    {
        var count = await auditLogService.CleanupExpiredLogsAsync(retentionDays, cancellationToken);
        return Ok(count);
    }
}
