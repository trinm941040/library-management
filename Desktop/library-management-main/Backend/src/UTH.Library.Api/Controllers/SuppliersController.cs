using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Api.Contracts.Suppliers;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Api.Controllers;

[ApiController, Authorize, Route("api/v1/suppliers")]
public sealed class SuppliersController(LibraryDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<SupplierPageResponse>> Get([FromQuery] SupplierFilterRequest request, CancellationToken ct)
    {
        var query = db.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, search) || EF.Functions.ILike(x.Name, search) || (x.ContactName != null && EF.Functions.ILike(x.ContactName, search)) || (x.Email != null && EF.Functions.ILike(x.Email, search)));
        }
        if (request.Status is not null) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(ct);
        var suppliers = await query.OrderBy(x => x.Code).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        var ids = suppliers.Select(x => x.Id).ToArray();
        var receiptIds = await db.StockReceipts.Where(x => ids.Contains(x.SupplierId)).Select(x => x.SupplierId).Distinct().ToListAsync(ct);
        return Ok(new SupplierPageResponse(suppliers.Select(x => ToResponse(x, receiptIds.Contains(x.Id))).ToArray(), request.PageNumber, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)));
    }

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<SupplierResponse>> GetById(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (supplier is null) return NotFound(Problem("Supplier was not found."));
        return Ok(ToResponse(supplier, await db.StockReceipts.AnyAsync(x => x.SupplierId == id, ct)));
    }

    [HttpPost, Authorize(Policy = Permissions.SuppliersCreate)]
    public async Task<ActionResult<SupplierResponse>> Create(SaveSupplierRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Suppliers.AnyAsync(x => x.Code == code, ct)) return Conflict(Problem("Supplier code already exists."));
        try
        {
            var supplier = Supplier.Create(code, request.Name, request.ContactName, request.Email, request.PhoneNumber, request.Address);
            db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, ToResponse(supplier, false));
        }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPut("{id:guid}"), Authorize(Policy = Permissions.SuppliersUpdate)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, SaveSupplierRequest request, CancellationToken ct)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (supplier is null) return NotFound(Problem("Supplier was not found."));
        if (request.ConcurrencyToken is not null && request.ConcurrencyToken != supplier.ConcurrencyToken) return Conflict(Problem("The supplier was modified by another request. Reload it and try again."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Suppliers.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Conflict(Problem("Supplier code already exists."));
        try { supplier.Update(code, request.Name, request.ContactName, request.Email, request.PhoneNumber, request.Address); await db.SaveChangesAsync(ct); return Ok(ToResponse(supplier, await db.StockReceipts.AnyAsync(x => x.SupplierId == id, ct))); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpDelete("{id:guid}"), Authorize(Policy = Permissions.SuppliersDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (supplier is null) return NotFound(Problem("Supplier was not found."));
        supplier.Deactivate(); await db.SaveChangesAsync(ct); return NoContent();
    }

    private static SupplierResponse ToResponse(Supplier supplier, bool hasReceipts) => new(supplier.Id, supplier.Code, supplier.Name, supplier.ContactName, supplier.Email, supplier.PhoneNumber, supplier.Address, supplier.Status, supplier.ConcurrencyToken, hasReceipts);
}
