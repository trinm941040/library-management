using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Api.Contracts.InventoryAudits;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;
namespace UTH.Library.Api.Controllers;
[ApiController, Authorize, Route("api/v1/inventory-audits")]
public sealed class InventoryAuditsController(LibraryDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.InventoryAuditsRead)]
    public async Task<ActionResult<InventoryAuditResponse>> Get(Guid id, CancellationToken ct) { var audit = await db.InventoryAudits.Include(x => db.InventoryAuditItems.Where(item => item.InventoryAuditId == id)).SingleOrDefaultAsync(x => x.Id == id, ct); return audit is null ? NotFound(Problem("Inventory audit was not found.")) : Ok(await Map(audit, ct)); }
    [HttpPost, Authorize(Policy = Permissions.InventoryAuditsCreate)]
    public async Task<ActionResult<InventoryAuditResponse>> Create(CreateInventoryAuditRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor)) return Unauthorized();
        if (!await db.Branches.AnyAsync(x => x.Id == request.BranchId && x.IsActive, ct)) return BadRequest(Problem("Branch is invalid or inactive."));
        var audit = InventoryAudit.Create(request.BranchId, actor, request.Notes, DateTime.UtcNow);
        var copies = db.BookCopies.Where(copy => db.Books.Any(book => book.Id == copy.BookId && book.Status == RecordStatus.Active) && db.Shelves.Any(shelf => shelf.Id == copy.ShelfId && db.Areas.Any(area => area.Id == shelf.AreaId && area.BranchId == request.BranchId)));
        if (request.ShelfId is not null) copies = copies.Where(copy => copy.ShelfId == request.ShelfId);
        else if (request.AreaId is not null) copies = copies.Where(copy => db.Shelves.Any(shelf => shelf.Id == copy.ShelfId && shelf.AreaId == request.AreaId));
        db.InventoryAudits.Add(audit); await db.InventoryAuditItems.AddRangeAsync(await copies.Select(copy => InventoryAuditItem.Create(audit.Id, copy.Id, copy.ShelfId)).ToListAsync(ct), ct); await db.SaveChangesAsync(ct); return CreatedAtAction(nameof(Get), new { id = audit.Id }, await Map(audit, ct));
    }
    [HttpPost("{id:guid}/scan"), Authorize(Policy = Permissions.InventoryAuditsUpdate)]
    public async Task<ActionResult<InventoryAuditResponse>> Scan(Guid id, ScanInventoryAuditRequest request, CancellationToken ct)
    {
        var audit = await db.InventoryAudits.SingleOrDefaultAsync(x => x.Id == id, ct); if (audit is null) return NotFound(Problem("Inventory audit was not found.")); if (audit.Status != InventoryAuditStatus.InProgress) return Conflict(Problem("Inventory audit is already closed."));
        var copy = await db.BookCopies.SingleOrDefaultAsync(x => x.Barcode == request.Barcode.Trim().ToUpperInvariant(), ct); if (copy is null) return NotFound(Problem("Barcode was not found."));
        var item = await db.InventoryAuditItems.SingleOrDefaultAsync(x => x.InventoryAuditId == id && x.BookCopyId == copy.Id, ct); if (item is null) return Conflict(Problem("Barcode is outside this audit scope."));
        if (request.ConcurrencyToken is not null && request.ConcurrencyToken != audit.ConcurrencyToken) return Conflict(Problem("The audit was modified by another request."));
        try { item.Scan(request.ActualShelfId, request.Result, DateTime.UtcNow); await db.SaveChangesAsync(ct); return Ok(await Map(audit, ct)); } catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message)); }
    }
    [HttpPost("{id:guid}/complete"), Authorize(Policy = Permissions.InventoryAuditsUpdate)]
    public async Task<ActionResult<InventoryAuditResponse>> Complete(Guid id, CompleteInventoryAuditRequest request, CancellationToken ct)
    { var audit = await db.InventoryAudits.SingleOrDefaultAsync(x => x.Id == id, ct); if (audit is null) return NotFound(Problem("Inventory audit was not found.")); if (request.ConcurrencyToken is not null && request.ConcurrencyToken != audit.ConcurrencyToken) return Conflict(Problem("The audit was modified by another request.")); if (await db.InventoryAuditItems.AnyAsync(x => x.InventoryAuditId == id && x.Result == AuditItemResult.Pending, ct)) return BadRequest(Problem("All expected copies must be scanned before completion.")); try { audit.Complete(DateTime.UtcNow); await db.SaveChangesAsync(ct); return Ok(await Map(audit, ct)); } catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message)); } }
    private async Task<InventoryAuditResponse> Map(InventoryAudit audit, CancellationToken ct) { var items = await db.InventoryAuditItems.Where(x => x.InventoryAuditId == audit.Id).Join(db.BookCopies, x => x.BookCopyId, copy => copy.Id, (x, copy) => new InventoryAuditItemResponse(x.Id, x.BookCopyId, copy.Barcode, x.ExpectedShelfId, x.ActualShelfId, x.Result, x.ScannedAtUtc)).ToArrayAsync(ct); return new(audit.Id, audit.BranchId, audit.Status, audit.StartedAtUtc, audit.CompletedAtUtc, audit.ConcurrencyToken, items.Length, items.Count(x => x.Result == AuditItemResult.Pending), items.Count(x => x.Result == AuditItemResult.Found), items.Count(x => x.Result == AuditItemResult.Missing), items.Count(x => x.Result == AuditItemResult.Misplaced), items); }
}
