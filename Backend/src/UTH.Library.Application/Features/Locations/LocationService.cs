using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Locations;

public sealed class LocationService(
    ILocationRepository repository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<LocationTreeModel>> GetHierarchyAsync(bool includeInactive, CancellationToken cancellationToken) =>
        (await repository.GetHierarchyAsync(includeInactive, cancellationToken)).Select(Map).ToArray();

    public async Task<IReadOnlyList<ShelfPickerModel>> GetActiveShelvesAsync(CancellationToken cancellationToken) =>
        (await repository.GetActiveShelvesAsync(cancellationToken))
        .Select(x => new ShelfPickerModel(x.Id, x.Code, x.Label, x.AreaId, x.AreaCode, x.AreaName, x.BranchId, x.BranchCode, x.BranchName))
        .ToArray();

    public async Task<LocationImpactModel> GetImpactAsync(LocationType type, Guid id, CancellationToken cancellationToken)
    {
        var impact = type switch
        {
            LocationType.Branch => await GetBranchImpactAsync(id, cancellationToken),
            LocationType.Area => await GetAreaImpactAsync(id, cancellationToken),
            LocationType.Shelf => await GetShelfImpactAsync(id, cancellationToken),
            _ => throw new RequestValidationException(new Dictionary<string, string[]> { ["type"] = ["Location type is invalid."] })
        };
        return Map(impact);
    }

    private async Task<LocationImpactSnapshot> GetBranchImpactAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(id, cancellationToken);
        return await repository.GetBranchImpactAsync(id, cancellationToken);
    }

    private async Task<LocationImpactSnapshot> GetAreaImpactAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await RequireAreaAsync(id, cancellationToken);
        return await repository.GetAreaImpactAsync(id, cancellationToken);
    }

    private async Task<LocationImpactSnapshot> GetShelfImpactAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await RequireShelfAsync(id, cancellationToken);
        return await repository.GetShelfImpactAsync(id, cancellationToken);
    }

    public async Task<LocationTreeModel> CreateBranchAsync(SaveBranchCommand command, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(command.Code);
        if (await repository.BranchCodeExistsAsync(code, null, cancellationToken))
            throw new ResourceConflictException("Branch code already exists.");
        var branch = Branch.Create(code, command.Name, command.Address, UtcNow());
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddBranchAsync(branch, ct);
            AddAudit("location.branch.created", branch.Id, null, branch);
            return Map(branch, new BranchReadinessSnapshot(0, 0), []);
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> UpdateBranchAsync(Guid id, SaveBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await RequireBranchAsync(id, cancellationToken);
        EnsureConcurrency(command.ConcurrencyToken, branch.ConcurrencyToken);
        var code = NormalizeCode(command.Code);
        if (await repository.BranchCodeExistsAsync(code, id, cancellationToken))
            throw new ResourceConflictException("Branch code already exists.");
        var before = JsonSerializer.Serialize(branch);
        branch.Update(code, command.Name, command.Address, UtcNow());
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            AddAudit("location.branch.updated", branch.Id, before, branch);
            var readiness = await repository.GetBranchReadinessAsync(branch.Id, null, null, ct);
            return Map(branch, readiness, []);
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> CreateAreaAsync(SaveAreaCommand command, CancellationToken cancellationToken)
    {
        _ = await RequireBranchAsync(command.BranchId, cancellationToken);
        var code = NormalizeCode(command.Code);
        if (await repository.AreaCodeExistsAsync(command.BranchId, code, null, cancellationToken))
            throw new ResourceConflictException("Area code already exists in this branch.");
        var area = Area.Create(command.BranchId, code, command.Name);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddAreaAsync(area, ct);
            AddAudit("location.area.created", area.Id, null, area);
            return Map(area, []);
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> UpdateAreaAsync(Guid id, SaveAreaCommand command, CancellationToken cancellationToken)
    {
        var area = await RequireAreaAsync(id, cancellationToken);
        EnsureConcurrency(command.ConcurrencyToken, area.ConcurrencyToken);
        if (command.BranchId != area.BranchId)
            throw new ResourceConflictException("Moving an area to another branch is not supported.");
        var code = NormalizeCode(command.Code);
        if (await repository.AreaCodeExistsAsync(area.BranchId, code, id, cancellationToken))
            throw new ResourceConflictException("Area code already exists in this branch.");
        var before = JsonSerializer.Serialize(area);
        area.Update(code, command.Name);
        return await unitOfWork.ExecuteAsync(ct =>
        {
            AddAudit("location.area.updated", area.Id, before, area);
            return Task.FromResult(Map(area, []));
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> CreateShelfAsync(SaveShelfCommand command, CancellationToken cancellationToken)
    {
        var area = await RequireAreaAsync(command.AreaId, cancellationToken);
        if (!area.IsActive) throw new ResourceConflictException("Shelf cannot be created in an inactive area.");
        var code = NormalizeCode(command.Code);
        if (await repository.ShelfCodeExistsAsync(command.AreaId, code, null, cancellationToken))
            throw new ResourceConflictException("Shelf code already exists in this area.");
        var shelf = Shelf.Create(command.AreaId, code, command.Label);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddShelfAsync(shelf, ct);
            AddAudit("location.shelf.created", shelf.Id, null, shelf);
            return Map(shelf);
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> UpdateShelfAsync(Guid id, SaveShelfCommand command, CancellationToken cancellationToken)
    {
        var shelf = await RequireShelfAsync(id, cancellationToken);
        EnsureConcurrency(command.ConcurrencyToken, shelf.ConcurrencyToken);
        if (command.AreaId != shelf.AreaId)
            throw new ResourceConflictException("Moving a shelf to another area is not supported.");
        var code = NormalizeCode(command.Code);
        if (await repository.ShelfCodeExistsAsync(shelf.AreaId, code, id, cancellationToken))
            throw new ResourceConflictException("Shelf code already exists in this area.");
        var before = JsonSerializer.Serialize(shelf);
        shelf.Update(code, command.Label);
        return await unitOfWork.ExecuteAsync(ct =>
        {
            AddAudit("location.shelf.updated", shelf.Id, before, shelf);
            return Task.FromResult(Map(shelf));
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> ChangeBranchStatusAsync(Guid id, ChangeLocationStatusCommand command, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var branch = await RequireBranchAsync(id, ct);
            EnsureConcurrency(command.ConcurrencyToken, branch.ConcurrencyToken);
            var readiness = await repository.GetBranchReadinessAsync(id, null, null, ct);
            if (branch.IsActive == command.IsActive)
                return Map(branch, readiness, []);

            if (command.IsActive)
            {
                if (!readiness.CanActivate)
                    throw new ResourceConflictException("Branch requires at least one active area with an active shelf before activation.");
            }
            else
            {
                EnsureNoImpact(await repository.GetBranchImpactAsync(id, ct));
            }

            var before = JsonSerializer.Serialize(branch);
            if (command.IsActive) branch.Activate(UtcNow()); else branch.Deactivate(UtcNow());
            AddAudit(command.IsActive ? "location.branch.activated" : "location.branch.deactivated", id, before, branch);
            return Map(branch, await repository.GetBranchReadinessAsync(id, null, null, ct), []);
        }, cancellationToken);
    }

    public async Task<LocationTreeModel> ChangeAreaStatusAsync(Guid id, ChangeLocationStatusCommand command, CancellationToken cancellationToken)
        => await unitOfWork.ExecuteAsync(ct => ChangeAreaStatusInTransactionAsync(id, command, ct), cancellationToken);

    private async Task<LocationTreeModel> ChangeAreaStatusInTransactionAsync(
        Guid id,
        ChangeLocationStatusCommand command,
        CancellationToken cancellationToken)
    {
        var area = await RequireAreaAsync(id, cancellationToken);
        EnsureConcurrency(command.ConcurrencyToken, area.ConcurrencyToken);
        var shelves = await repository.GetShelvesByAreaAsync(id, cancellationToken);
        if (area.IsActive == command.IsActive) return Map(area, shelves);

        if (!command.IsActive)
        {
            EnsureNoImpact(await repository.GetAreaImpactAsync(id, cancellationToken));
            var branch = await RequireBranchAsync(area.BranchId, cancellationToken);
            var readiness = await repository.GetBranchReadinessAsync(area.BranchId, id, null, cancellationToken);
            if (branch.IsActive && !readiness.CanActivate)
                throw new ResourceConflictException("This is the last active area with an active shelf in an active branch.");
        }

        var before = JsonSerializer.Serialize(area);
        if (command.IsActive) area.Activate();
        else
        {
            area.Deactivate();
            foreach (var shelf in shelves.Where(x => x.Status == ShelfStatus.Active))
            {
                var shelfBefore = JsonSerializer.Serialize(shelf);
                shelf.Deactivate();
                AddAudit("location.shelf.deactivated", shelf.Id, shelfBefore, shelf);
            }
        }

        AddAudit(command.IsActive ? "location.area.activated" : "location.area.deactivated", id, before, area);
        return Map(area, shelves);
    }

    public async Task<LocationTreeModel> ChangeShelfStatusAsync(Guid id, ChangeLocationStatusCommand command, CancellationToken cancellationToken)
        => await unitOfWork.ExecuteAsync(ct => ChangeShelfStatusInTransactionAsync(id, command, ct), cancellationToken);

    private async Task<LocationTreeModel> ChangeShelfStatusInTransactionAsync(
        Guid id,
        ChangeLocationStatusCommand command,
        CancellationToken cancellationToken)
    {
        var shelf = await RequireShelfAsync(id, cancellationToken);
        EnsureConcurrency(command.ConcurrencyToken, shelf.ConcurrencyToken);
        var currentlyActive = shelf.Status == ShelfStatus.Active;
        if (currentlyActive == command.IsActive) return Map(shelf);

        var area = await RequireAreaAsync(shelf.AreaId, cancellationToken);
        if (command.IsActive && !area.IsActive)
            throw new ResourceConflictException("Shelf cannot be activated while its area is inactive.");
        if (!command.IsActive)
        {
            EnsureNoImpact(await repository.GetShelfImpactAsync(id, cancellationToken));
            var branch = await RequireBranchAsync(area.BranchId, cancellationToken);
            var readiness = await repository.GetBranchReadinessAsync(area.BranchId, null, id, cancellationToken);
            if (branch.IsActive && !readiness.CanActivate)
                throw new ResourceConflictException("This is the last active shelf in an active branch.");
        }

        var before = JsonSerializer.Serialize(shelf);
        if (command.IsActive) shelf.Activate(); else shelf.Deactivate();
        AddAudit(command.IsActive ? "location.shelf.activated" : "location.shelf.deactivated", id, before, shelf);
        return Map(shelf);
    }

    private async Task<Branch> RequireBranchAsync(Guid id, CancellationToken ct) =>
        await repository.GetBranchAsync(id, ct) ?? throw new ResourceNotFoundException("Branch was not found.");

    private async Task<Area> RequireAreaAsync(Guid id, CancellationToken ct) =>
        await repository.GetAreaAsync(id, ct) ?? throw new ResourceNotFoundException("Area was not found.");

    private async Task<Shelf> RequireShelfAsync(Guid id, CancellationToken ct) =>
        await repository.GetShelfAsync(id, ct) ?? throw new ResourceNotFoundException("Shelf was not found.");

    private static void EnsureConcurrency(Guid? expected, Guid actual)
    {
        if (expected is null || expected == Guid.Empty)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["concurrencyToken"] = ["Concurrency token is required."] });
        if (expected != actual)
            throw new OptimisticConcurrencyException("The location was modified by another request. Reload it and try again.");
    }

    private static void EnsureNoImpact(LocationImpactSnapshot impact)
    {
        if (!impact.HasBlockingReferences) return;
        throw new ResourceConflictException(
            $"Location is still referenced: {impact.EmployeeCount} employee(s), {impact.BookCopyCount} book copy/copies, " +
            $"{impact.ActiveInventoryAuditCount} active inventory audit(s), {impact.EditableStockReceiptCount} editable stock receipt(s).");
    }

    private void AddAudit(string action, Guid entityId, string? before, object after) =>
        unitOfWork.AddAuditLog(AuditLog.Create(
            requestContext.UserId,
            action,
            "Location",
            entityId,
            before,
            JsonSerializer.Serialize(after),
            UtcNow(),
            requestContext.CorrelationId,
            requestContext.IpAddress));

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static LocationTreeModel Map(BranchLocationSnapshot snapshot) =>
        Map(snapshot.Branch, snapshot.Readiness, snapshot.Areas.Select(Map).ToArray());

    private static LocationTreeModel Map(Branch branch, BranchReadinessSnapshot readiness, IReadOnlyList<LocationTreeModel> children) =>
        new(branch.Id, LocationType.Branch, branch.Code, branch.Name, branch.Address, branch.IsActive, null,
            branch.ConcurrencyToken, Map(readiness), children);

    private static LocationTreeModel Map(AreaLocationSnapshot snapshot) => Map(snapshot.Area, snapshot.Shelves);

    private static LocationTreeModel Map(Area area, IReadOnlyList<Shelf> shelves) =>
        new(area.Id, LocationType.Area, area.Code, area.Name, null, area.IsActive, area.BranchId,
            area.ConcurrencyToken, null, shelves.Select(Map).ToArray());

    private static LocationTreeModel Map(Shelf shelf) =>
        new(shelf.Id, LocationType.Shelf, shelf.Code, shelf.Label, null, shelf.Status == ShelfStatus.Active,
            shelf.AreaId, shelf.ConcurrencyToken, null, []);

    private static BranchReadinessModel Map(BranchReadinessSnapshot readiness)
    {
        var missing = new List<string>();
        if (readiness.ActiveAreaCount == 0) missing.Add("At least one active area is required.");
        if (readiness.ActiveShelfCount == 0) missing.Add("At least one active shelf in an active area is required.");
        return new BranchReadinessModel(readiness.CanActivate, readiness.ActiveAreaCount, readiness.ActiveShelfCount, missing);
    }

    private static LocationImpactModel Map(LocationImpactSnapshot impact) =>
        new(impact.EmployeeCount, impact.BookCopyCount, impact.ActiveInventoryAuditCount,
            impact.EditableStockReceiptCount, impact.HasBlockingReferences);
}
