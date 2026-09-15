using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Api.Contracts.Copies;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Api.Controllers;

[ApiController, Authorize, Route("api/v1/copies")]
public sealed class CopiesController(LibraryDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyPageResponse>> Get([FromQuery] CopyFilterRequest request, CancellationToken ct)
    {
        var query = CopyQuery();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = $"%{request.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Copy.Barcode, search) || EF.Functions.ILike(x.Book.Title, search));
        }
        if (request.BookId is not null) query = query.Where(x => x.Copy.BookId == request.BookId);
        if (request.ShelfId is not null) query = query.Where(x => x.Copy.ShelfId == request.ShelfId);
        if (request.BranchId is not null) query = query.Where(x => x.Branch != null && x.Branch.Id == request.BranchId);
        if (request.Condition is not null) query = query.Where(x => x.Copy.Condition == request.Condition);
        if (request.Status is not null) query = query.Where(x => x.Copy.Status == request.Status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Copy.Barcode).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return Ok(new CopyPageResponse(items.Select(x => ToResponse(x.Copy, x.Book, x.Shelf, x.Branch)).ToArray(), request.PageNumber, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)));
    }

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await CopyQuery().SingleOrDefaultAsync(x => x.Copy.Id == id, ct);
        return item is null ? NotFound(Problem("Book copy was not found.")) : Ok(ToResponse(item.Copy, item.Book, item.Shelf, item.Branch));
    }

    [HttpGet("barcode/{barcode}"), Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyResponse>> GetByBarcode(string barcode, CancellationToken ct)
    {
        var normalized = barcode.Trim().ToUpperInvariant();
        var item = await CopyQuery().SingleOrDefaultAsync(x => x.Copy.Barcode == normalized, ct);
        return item is null ? NotFound(Problem("Book copy was not found.")) : Ok(ToResponse(item.Copy, item.Book, item.Shelf, item.Branch));
    }

    [HttpPost, Authorize(Policy = Permissions.CopiesCreate)]
    public async Task<ActionResult<CopyResponse>> Create(CreateCopyRequest request, CancellationToken ct)
    {
        if (!await db.Books.AnyAsync(x => x.Id == request.BookId && x.Status == RecordStatus.Active, ct)) return BadRequest(Problem("Book is invalid or inactive."));
        var barcode = request.Barcode.Trim().ToUpperInvariant();
        if (await db.BookCopies.AnyAsync(x => x.Barcode == barcode, ct)) return Conflict(Problem("Barcode already exists."));
        if (request.ShelfId is not null && !await ActiveShelfExists(request.ShelfId.Value, ct)) return BadRequest(Problem("Shelf is invalid or inactive."));
        try
        {
            var copy = BookCopy.Create(request.BookId, barcode, request.Condition, request.ShelfId, request.StockReceiptItemId, timeProvider.GetUtcNow().UtcDateTime);
            db.BookCopies.Add(copy);
            await db.SaveChangesAsync(ct);
            var item = await CopyQuery().SingleAsync(x => x.Copy.Id == copy.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = copy.Id }, ToResponse(item.Copy, item.Book, item.Shelf, item.Branch));
        }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPatch("{id:guid}/status"), Authorize(Policy = Permissions.CopiesUpdate)]
    public async Task<ActionResult<CopyResponse>> UpdateStatus(Guid id, UpdateCopyStatusRequest request, CancellationToken ct)
    {
        var copy = await db.BookCopies.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (copy is null) return NotFound(Problem("Book copy was not found."));
        if (request.ConcurrencyToken is not null && copy.ConcurrencyToken != request.ConcurrencyToken) return Conflict(Problem("The copy was modified by another request. Reload it and try again."));
        try { copy.ChangeStatus(request.Status); await db.SaveChangesAsync(ct); var item = await CopyQuery().SingleAsync(x => x.Copy.Id == id, ct); return Ok(ToResponse(item.Copy, item.Book, item.Shelf, item.Branch)); }
        catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message)); }
    }

    [HttpPatch("{id:guid}/relocate"), Authorize(Policy = Permissions.CopiesUpdate)]
    public async Task<ActionResult<CopyResponse>> Relocate(Guid id, RelocateCopyRequest request, CancellationToken ct)
    {
        var copy = await db.BookCopies.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (copy is null) return NotFound(Problem("Book copy was not found."));
        if (request.ConcurrencyToken is not null && copy.ConcurrencyToken != request.ConcurrencyToken) return Conflict(Problem("The copy was modified by another request. Reload it and try again."));
        if (!await ActiveShelfExists(request.ShelfId, ct)) return BadRequest(Problem("Shelf is invalid or inactive."));
        try { copy.Relocate(request.ShelfId); await db.SaveChangesAsync(ct); var item = await CopyQuery().SingleAsync(x => x.Copy.Id == id, ct); return Ok(ToResponse(item.Copy, item.Book, item.Shelf, item.Branch)); }
        catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message)); }
    }

    private IQueryable<CopyProjection> CopyQuery() =>
        from copy in db.BookCopies
        join book in db.Books on copy.BookId equals book.Id
        join shelf in db.Shelves on copy.ShelfId equals (Guid?)shelf.Id into shelfGroup
        from shelf in shelfGroup.DefaultIfEmpty()
        join area in db.Areas on shelf.AreaId equals area.Id into areaGroup
        from area in areaGroup.DefaultIfEmpty()
        join branch in db.Branches on area.BranchId equals branch.Id into branchGroup
        from branch in branchGroup.DefaultIfEmpty()
        select new CopyProjection(copy, book, shelf, branch);

    private Task<bool> ActiveShelfExists(Guid shelfId, CancellationToken ct) => db.Shelves.AnyAsync(shelf => shelf.Id == shelfId && shelf.Status == ShelfStatus.Active && db.Areas.Any(area => area.Id == shelf.AreaId && db.Branches.Any(branch => branch.Id == area.BranchId && branch.IsActive)), ct);

    private static CopyResponse ToResponse(BookCopy copy, Book book, Shelf? shelf, Branch? branch) => new(copy.Id, copy.BookId, book.Title, copy.Barcode, copy.Condition, copy.Status, copy.AcquiredAtUtc, copy.ShelfId, shelf?.Code, branch?.Id, branch?.Code, copy.StockReceiptItemId, copy.ConcurrencyToken);
    private sealed record CopyProjection(BookCopy Copy, Book Book, Shelf? Shelf, Branch? Branch);
}
