using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.InventoryAudits;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.InventoryAudits;
using UTH.Library.Application.Features.Locations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory-audits")]
public sealed class InventoryAuditsController(InventoryAuditService service, LocationService locations) : ControllerBase
{
    [HttpGet("locations")]
    [Authorize(Policy = Permissions.InventoryAuditsRead)]
    public async Task<IActionResult> GetLocations(CancellationToken cancellationToken) =>
        Ok(await locations.GetHierarchyAsync(false, cancellationToken));

    [HttpGet]
    [Authorize(Policy = Permissions.InventoryAuditsRead)]
    public async Task<ActionResult<object>> Get([FromQuery] InventoryAuditFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await service.GetPageAsync(new InventoryAuditQuery(request.BranchId,
            request.Status, request.PageNumber, request.PageSize), cancellationToken);
        return Ok(new { page.Items, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.InventoryAuditsRead)]
    public async Task<ActionResult<InventoryAuditModel>> GetById(Guid id, CancellationToken cancellationToken) =>
        (await service.GetAsync(id, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [HttpPost]
    [Authorize(Policy = Permissions.InventoryAuditsCreate)]
    public async Task<ActionResult<InventoryAuditModel>> Create(CreateInventoryAuditRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(new CreateInventoryAuditCommand(request.BranchId,
            request.AreaId, request.ShelfId, request.Notes), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.InventoryAuditsCreate)]
    public async Task<ActionResult<InventoryAuditModel>> Start(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result is null ? NotFound() : result.Status == InventoryAuditStatus.InProgress
            ? Ok(result) : Conflict();
    }

    [HttpPost("{id:guid}/scan")]
    [Authorize(Policy = Permissions.InventoryAuditsScan)]
    public async Task<ActionResult<InventoryAuditModel>> Scan(Guid id, ScanInventoryAuditRequest request,
        CancellationToken cancellationToken) => Ok(await service.ScanAsync(id,
            new ScanInventoryAuditCommand(request.Barcode, request.ActualShelfId,
                request.ActualStatus, request.ActualCondition, request.ConcurrencyToken), cancellationToken));

    [HttpGet("{id:guid}/reconcile")]
    [Authorize(Policy = Permissions.InventoryAuditsRead)]
    public async Task<ActionResult<InventoryAuditModel>> Reconcile(Guid id, CancellationToken cancellationToken) =>
        (await service.ReconcileAsync(id, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.InventoryAuditsComplete)]
    public async Task<ActionResult<InventoryAuditModel>> Complete(Guid id,
        CompleteInventoryAuditRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CompleteAsync(id, new CompleteInventoryAuditCommand(
            request.ConcurrencyToken, request.AcknowledgeDiscrepancies), cancellationToken));

    [HttpPost("{id:guid}/apply")]
    [Authorize(Policy = Permissions.InventoryAuditsApply)]
    public async Task<IActionResult> Apply(Guid id, ApplyInventoryCorrectionsRequest request,
        CancellationToken cancellationToken)
    {
        var count = await service.ApplyCorrectionsAsync(id, new ApplyInventoryCorrectionsCommand(
            request.Corrections.Select(row => new ApplyInventoryCorrectionCommand(row.BookCopyId,
                row.ConcurrencyToken, row.ShelfId, row.Status, row.Condition)).ToArray()), cancellationToken);
        return Ok(new { appliedCount = count });
    }

    [HttpGet("{id:guid}/export")]
    [Authorize(Policy = Permissions.InventoryAuditsExport)]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken)
    {
        var audit = await service.GetAsync(id, cancellationToken);
        if (audit is null) return NotFound();
        static string Csv(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Length > 0 && "=+-@".Contains(text[0])) text = "'" + text;
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
        var csv = new StringBuilder("barcode,book_title,is_expected,expected_shelf,actual_shelf,expected_status,actual_status,expected_condition,actual_condition,result,scanned_at_utc\r\n");
        foreach (var item in audit.Items)
            csv.AppendJoin(',', Csv(item.Barcode), Csv(item.BookTitle), item.IsExpected ? "true" : "false",
                Csv(item.ExpectedShelfId?.ToString()), Csv(item.ActualShelfId?.ToString()),
                Csv(item.ExpectedStatus.ToString()), Csv(item.ActualStatus?.ToString()),
                Csv(item.ExpectedCondition.ToString()), Csv(item.ActualCondition?.ToString()),
                Csv(item.Result.ToString()), Csv(item.ScannedAtUtc?.ToString("O"))).Append("\r\n");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8",
            $"inventory-audit-{id:N}.csv");
    }
}
