using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Api.Contracts.StockReceipts;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Api.Controllers;

[ApiController, Authorize, Route("api/v1/stock-receipts")]
public sealed class StockReceiptsController(LibraryDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.StockReceiptsRead)]
    public async Task<ActionResult<StockReceiptPageResponse>> Get([FromQuery] StockReceiptFilterRequest request, CancellationToken ct)
    {
        var query = db.StockReceipts.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => EF.Functions.ILike(x.ReceiptNumber, $"%{request.Search.Trim()}%"));
        if (request.SupplierId is not null) query = query.Where(x => x.SupplierId == request.SupplierId);
        if (request.BranchId is not null) query = query.Where(x => x.BranchId == request.BranchId);
        if (request.Status is not null) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(ct);
        var receipts = await query.OrderByDescending(x => x.ReceivedAtUtc).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return Ok(new StockReceiptPageResponse(receipts.Select(Map).ToArray(), request.PageNumber, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)));
    }

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.StockReceiptsRead)]
    public async Task<ActionResult<StockReceiptResponse>> GetById(Guid id, CancellationToken ct)
    {
        var receipt = await db.StockReceipts.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        return receipt is null ? NotFound(Problem("Stock receipt was not found.")) : Ok(Map(receipt));
    }

    [HttpPost, Authorize(Policy = Permissions.StockReceiptsCreate)]
    public async Task<ActionResult<StockReceiptResponse>> Create(SaveStockReceiptRequest request, CancellationToken ct)
    {
        var actor = GetActor();
        if (actor is null) return Unauthorized();
        var validation = await ValidateReferencesAsync(request, ct);
        if (validation is not null) return BadRequest(Problem(validation));
        try
        {
            var receipt = StockReceipt.Create($"GR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..25], request.SupplierId, request.BranchId, actor.Value, request.ReceivedAtUtc, request.Notes);
            db.StockReceipts.Add(receipt);
            foreach (var item in request.Items) db.StockReceiptItems.Add(StockReceiptItem.Create(receipt.Id, item.BookId, item.ExpectedQuantity, item.ReceivedQuantity, item.DamagedQuantity, item.UnitCost));
            await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, Map(await db.StockReceipts.Include(x => x.Items).SingleAsync(x => x.Id == receipt.Id, ct)));
        }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPut("{id:guid}"), Authorize(Policy = Permissions.StockReceiptsUpdate)]
    public async Task<ActionResult<StockReceiptResponse>> Update(Guid id, SaveStockReceiptRequest request, CancellationToken ct)
    {
        var receipt = await db.StockReceipts.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (receipt is null) return NotFound(Problem("Stock receipt was not found."));
        if (request.ConcurrencyToken is not null && request.ConcurrencyToken != receipt.ConcurrencyToken) return Conflict(Problem("The stock receipt was modified by another request. Reload it and try again."));
        var validation = await ValidateReferencesAsync(request, ct);
        if (validation is not null) return BadRequest(Problem(validation));
        try
        {
            receipt.Update(request.SupplierId, request.BranchId, request.ReceivedAtUtc, request.Notes);
            db.StockReceiptItems.RemoveRange(receipt.Items);
            foreach (var item in request.Items) db.StockReceiptItems.Add(StockReceiptItem.Create(receipt.Id, item.BookId, item.ExpectedQuantity, item.ReceivedQuantity, item.DamagedQuantity, item.UnitCost));
            await db.SaveChangesAsync(ct);
            return Ok(Map(await db.StockReceipts.AsNoTracking().Include(x => x.Items).SingleAsync(x => x.Id == id, ct)));
        }
        catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    private async Task<string?> ValidateReferencesAsync(SaveStockReceiptRequest request, CancellationToken ct)
    {
        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.Status == RecordStatus.Active, ct)) return "Supplier is invalid or inactive.";
        if (!await db.Branches.AnyAsync(x => x.Id == request.BranchId && x.IsActive, ct)) return "Branch is invalid or inactive.";
        var ids = request.Items.Select(x => x.BookId).Distinct().ToArray();
        if (ids.Length != request.Items.Count || await db.Books.CountAsync(x => ids.Contains(x.Id) && x.Status == RecordStatus.Active, ct) != ids.Length) return "Receipt items must reference distinct active books.";
        return null;
    }

    private Guid? GetActor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
    private static StockReceiptResponse Map(StockReceipt receipt) => new(receipt.Id, receipt.ReceiptNumber, receipt.SupplierId, string.Empty, receipt.BranchId, string.Empty, receipt.ReceivedByUserId, receipt.Status, receipt.ReceivedAtUtc, receipt.Notes, receipt.ConcurrencyToken, receipt.Items.Sum(x => x.ReceivedQuantity), receipt.Items.Sum(x => (x.UnitCost ?? 0) * x.ReceivedQuantity), receipt.Items.Select(x => new StockReceiptItemResponse(x.Id, x.BookId, string.Empty, string.Empty, x.ExpectedQuantity, x.ReceivedQuantity, x.DamagedQuantity, x.UnitCost, (x.UnitCost ?? 0) * x.ReceivedQuantity, x.ConcurrencyToken)).ToArray());
}
