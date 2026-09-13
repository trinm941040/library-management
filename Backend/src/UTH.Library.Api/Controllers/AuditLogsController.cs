using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using UTH.Library.Api.Contracts.AuditLogs;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.AuditLogs;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.AuditLogsRead)]
[Route("api/v1/audit-log")]
public sealed class AuditLogsController(AuditLogService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPageResponse>> Get(
        [FromQuery] AuditLogFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromUtc is DateTime from && request.ToUtc is DateTime to && to <= from)
            return BadRequest(new ProblemDetails { Detail = "Thời điểm kết thúc phải sau thời điểm bắt đầu." });
        if (!string.IsNullOrWhiteSpace(request.IpAddress) && !IPAddress.TryParse(request.IpAddress, out _))
            return BadRequest(new ProblemDetails { Detail = "Địa chỉ IP không hợp lệ." });

        var page = await service.GetPageAsync(ToQuery(request), cancellationToken);
        return Ok(new AuditLogPageResponse(
            page.Items.Select(Map).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(Map(item));
    }

    [HttpGet("entities/{entityType}/{entityId:guid}")]
    [ProducesResponseType(typeof(AuditLogPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPageResponse>> GetEntityHistory(
        string entityType,
        Guid entityId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await service.GetPageAsync(
            new AuditLogQuery(null, null, entityType, entityId, null, null, null, null, pageNumber, pageSize),
            cancellationToken);
        return Ok(new AuditLogPageResponse(
            page.Items.Select(Map).ToArray(), page.PageNumber, page.PageSize, page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize)));
    }

    private static AuditLogQuery ToQuery(AuditLogFilterRequest request) => new(
        request.ActorUserId, request.Action, request.EntityType, request.EntityId,
        request.CorrelationId, request.IpAddress, request.FromUtc, request.ToUtc,
        request.PageNumber, request.PageSize);

    private static AuditLogResponse Map(AuditLogModel item) => new(
        item.Id, item.ActorUserId, item.ActorName, item.Action, item.EntityType, item.EntityId,
        item.BeforeJson, item.AfterJson, item.CreatedAtUtc, item.CorrelationId, item.IpAddress);
}
