using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Locations;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Locations;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/locations")]
public sealed class LocationsController(LocationService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.LocationsRead)]
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> Get(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default) =>
        Ok((await service.GetHierarchyAsync(includeInactive, cancellationToken)).Select(Map).ToArray());

    [HttpGet("active-shelves")]
    [Authorize(Policy = Permissions.LocationsRead)]
    public async Task<ActionResult<IReadOnlyList<ShelfPickerResponse>>> GetActiveShelves(CancellationToken cancellationToken) =>
        Ok((await service.GetActiveShelvesAsync(cancellationToken))
            .Select(x => new ShelfPickerResponse(x.Id, x.Code, x.Label, x.AreaId, x.AreaCode, x.AreaName,
                x.BranchId, x.BranchCode, x.BranchName)).ToArray());

    [HttpGet("{type}/{id:guid}/impact")]
    [Authorize(Policy = Permissions.LocationsRead)]
    public async Task<ActionResult<LocationImpactResponse>> GetImpact(
        LocationType type,
        Guid id,
        CancellationToken cancellationToken)
    {
        var impact = await service.GetImpactAsync(type, id, cancellationToken);
        return Ok(new LocationImpactResponse(impact.EmployeeCount, impact.BookCopyCount,
            impact.ActiveInventoryAuditCount, impact.EditableStockReceiptCount, impact.HasBlockingReferences));
    }

    [HttpPost("branches")]
    [Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateBranch(SaveBranchRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateBranchAsync(
            new SaveBranchCommand(request.Code, request.Name, request.Address), cancellationToken);
        return CreatedAtAction(nameof(Get), Map(result));
    }

    [HttpPut("branches/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> UpdateBranch(Guid id, SaveBranchRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateBranchAsync(id,
            new SaveBranchCommand(request.Code, request.Name, request.Address, request.ConcurrencyToken), cancellationToken)));

    [HttpPost("areas")]
    [Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateArea(SaveAreaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAreaAsync(
            new SaveAreaCommand(request.BranchId, request.Code, request.Name), cancellationToken);
        return CreatedAtAction(nameof(Get), Map(result));
    }

    [HttpPut("areas/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> UpdateArea(Guid id, SaveAreaRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateAreaAsync(id,
            new SaveAreaCommand(request.BranchId, request.Code, request.Name, request.ConcurrencyToken), cancellationToken)));

    [HttpPost("shelves")]
    [Authorize(Policy = Permissions.LocationsCreate)]
    public async Task<ActionResult<LocationResponse>> CreateShelf(SaveShelfRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateShelfAsync(
            new SaveShelfCommand(request.AreaId, request.Code, request.Label), cancellationToken);
        return CreatedAtAction(nameof(Get), Map(result));
    }

    [HttpPut("shelves/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> UpdateShelf(Guid id, SaveShelfRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateShelfAsync(id,
            new SaveShelfCommand(request.AreaId, request.Code, request.Label, request.ConcurrencyToken), cancellationToken)));

    [HttpPost("branches/{id:guid}/activate")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> ActivateBranch(Guid id, ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeBranchStatusAsync(id, new ChangeLocationStatusCommand(true, request.ConcurrencyToken), cancellationToken)));

    [HttpDelete("branches/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<ActionResult<LocationResponse>> DeactivateBranch(Guid id, [FromBody] ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeBranchStatusAsync(id, new ChangeLocationStatusCommand(false, request.ConcurrencyToken), cancellationToken)));

    [HttpPost("areas/{id:guid}/activate")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> ActivateArea(Guid id, ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeAreaStatusAsync(id, new ChangeLocationStatusCommand(true, request.ConcurrencyToken), cancellationToken)));

    [HttpDelete("areas/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<ActionResult<LocationResponse>> DeactivateArea(Guid id, [FromBody] ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeAreaStatusAsync(id, new ChangeLocationStatusCommand(false, request.ConcurrencyToken), cancellationToken)));

    [HttpPost("shelves/{id:guid}/activate")]
    [Authorize(Policy = Permissions.LocationsUpdate)]
    public async Task<ActionResult<LocationResponse>> ActivateShelf(Guid id, ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeShelfStatusAsync(id, new ChangeLocationStatusCommand(true, request.ConcurrencyToken), cancellationToken)));

    [HttpDelete("shelves/{id:guid}")]
    [Authorize(Policy = Permissions.LocationsDeactivate)]
    public async Task<ActionResult<LocationResponse>> DeactivateShelf(Guid id, [FromBody] ChangeLocationStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeShelfStatusAsync(id, new ChangeLocationStatusCommand(false, request.ConcurrencyToken), cancellationToken)));

    private static LocationResponse Map(LocationTreeModel model) =>
        new(model.Id, model.Type, model.Code, model.Name, model.Address, model.IsActive, model.ParentId,
            model.ConcurrencyToken,
            model.Readiness is null ? null : new BranchReadinessResponse(model.Readiness.CanActivate,
                model.Readiness.ActiveAreaCount, model.Readiness.ActiveShelfCount, model.Readiness.MissingRequirements),
            model.Children.Select(Map).ToArray());
}
