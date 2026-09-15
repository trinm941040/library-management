using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Api.Contracts.Locations;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Api.Controllers;

[ApiController, Authorize, Route("api/v1/locations")]
public sealed class LocationsController(LibraryDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.LocationsRead)]
    public async Task<ActionResult<IReadOnlyCollection<LocationResponse>>> Get(CancellationToken ct)
    {
        var branches = await db.Branches.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        var areas = await db.Areas.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        var shelves = await db.Shelves.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        var shelfByArea = shelves.GroupBy(x => x.AreaId).ToDictionary(x => x.Key, x => (IReadOnlyCollection<LocationResponse>)x.Select(ToResponse).ToArray());
        var areaByBranch = areas.GroupBy(x => x.BranchId).ToDictionary(x => x.Key, x => (IReadOnlyCollection<LocationResponse>)x.Select(area => ToResponse(area, shelfByArea.GetValueOrDefault(area.Id))).ToArray());
        return Ok(branches.Select(branch => ToResponse(branch, areaByBranch.GetValueOrDefault(branch.Id))).ToArray());
    }

    [HttpPost("branches"), Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateBranch(SaveBranchRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Branches.AnyAsync(x => x.Code == code, ct)) return Conflict(Problem("Branch code already exists."));
        try
        {
            var branch = Branch.Create(code, request.Name, request.Address, timeProvider.GetUtcNow().UtcDateTime);
            db.Branches.Add(branch);
            await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Get), null, ToResponse(branch));
        }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPost("areas"), Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateArea(SaveAreaRequest request, CancellationToken ct)
    {
        if (!await db.Branches.AnyAsync(x => x.Id == request.BranchId, ct)) return BadRequest(Problem("Branch was not found."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Areas.AnyAsync(x => x.BranchId == request.BranchId && x.Code == code, ct)) return Conflict(Problem("Area code already exists in this branch."));
        try { var area = Area.Create(request.BranchId, code, request.Name); db.Areas.Add(area); await db.SaveChangesAsync(ct); return Ok(ToResponse(area)); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPost("shelves"), Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateShelf(SaveShelfRequest request, CancellationToken ct)
    {
        if (!await db.Areas.AnyAsync(x => x.Id == request.AreaId, ct)) return BadRequest(Problem("Area was not found."));
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Shelves.AnyAsync(x => x.AreaId == request.AreaId && x.Code == code, ct)) return Conflict(Problem("Shelf code already exists in this area."));
        try { var shelf = Shelf.Create(request.AreaId, code, request.Label); db.Shelves.Add(shelf); await db.SaveChangesAsync(ct); return Ok(ToResponse(shelf)); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message)); }
    }

    [HttpPatch("branches/{id:guid}/status"), Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> UpdateBranchStatus(Guid id, CancellationToken ct)
    {
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (branch is null) return NotFound(Problem("Branch was not found."));
        var ready = await db.Areas.AnyAsync(area => area.BranchId == id && db.Shelves.Any(shelf => shelf.AreaId == area.Id && shelf.Status == ShelfStatus.Active), ct);
        if (!ready) return Conflict(Problem("Branch requires at least one active area with an active shelf."));
        branch.Activate(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(branch));
    }

    [HttpDelete("branches/{id:guid}"), Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<IActionResult> DeactivateBranch(Guid id, CancellationToken ct)
    {
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (branch is null) return NotFound(Problem("Branch was not found."));
        var referenced = await db.Employees.AnyAsync(x => x.BranchId == id && x.Status != EmploymentStatus.Terminated, ct);
        if (referenced) return Conflict(Problem("Branch is still used by active employees."));
        branch.Deactivate(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("areas/{id:guid}"), Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<IActionResult> DeactivateArea(Guid id, CancellationToken ct)
    {
        var area = await db.Areas.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (area is null) return NotFound(Problem("Area was not found."));
        var referenced = await db.BookCopies.AnyAsync(copy => copy.ShelfId != null && db.Shelves.Any(shelf => shelf.Id == copy.ShelfId && shelf.AreaId == id), ct);
        if (referenced) return Conflict(Problem("Area still contains shelves used by book copies."));
        var shelves = await db.Shelves.Where(x => x.AreaId == id && x.Status == ShelfStatus.Active).ToListAsync(ct);
        shelves.ForEach(shelf => shelf.Deactivate());
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("shelves/{id:guid}"), Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<IActionResult> DeactivateShelf(Guid id, CancellationToken ct)
    {
        var shelf = await db.Shelves.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (shelf is null) return NotFound(Problem("Shelf was not found."));
        if (await db.BookCopies.AnyAsync(x => x.ShelfId == id, ct)) return Conflict(Problem("Shelf is still used by book copies."));
        shelf.Deactivate();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static LocationResponse ToResponse(Branch branch, IReadOnlyCollection<LocationResponse>? children = null) => new(branch.Id, branch.Code, branch.Name, branch.Address, branch.IsActive, null, null, null, Guid.Empty, children);
    private static LocationResponse ToResponse(Area area, IReadOnlyCollection<LocationResponse>? children = null) => new(area.Id, area.Code, area.Name, null, true, area.BranchId, null, null, area.ConcurrencyToken, children);
    private static LocationResponse ToResponse(Shelf shelf) => new(shelf.Id, shelf.Code, shelf.Label, null, shelf.Status == ShelfStatus.Active, null, shelf.AreaId, shelf.Status, shelf.ConcurrencyToken);
}
