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
            _ => throw new RequestValidationException(new Dictionary<string, string[]> { ["type"] = ["Loại vị trí không hợp lệ."] })
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
            throw new ResourceConflictException("Mã chi nhánh đã tồn tại.");
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
            throw new ResourceConflictException("Mã chi nhánh đã tồn tại.");
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
            throw new ResourceConflictException("Mã khu vực đã tồn tại trong chi nhánh này.");
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
            throw new ResourceConflictException("Không hỗ trợ chuyển khu vực sang chi nhánh khác.");
        var code = NormalizeCode(command.Code);
        if (await repository.AreaCodeExistsAsync(area.BranchId, code, id, cancellationToken))
            throw new ResourceConflictException("Mã khu vực đã tồn tại trong chi nhánh này.");
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
        if (!area.IsActive) throw new ResourceConflictException("Không thể tạo kệ trong khu vực đã ngừng hoạt động.");
        var code = NormalizeCode(command.Code);
        if (await repository.ShelfCodeExistsAsync(command.AreaId, code, null, cancellationToken))
            throw new ResourceConflictException("Mã kệ đã tồn tại trong khu vực này.");
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
            throw new ResourceConflictException("Không hỗ trợ chuyển kệ sang khu vực khác.");
        var code = NormalizeCode(command.Code);
        if (await repository.ShelfCodeExistsAsync(shelf.AreaId, code, id, cancellationToken))
            throw new ResourceConflictException("Mã kệ đã tồn tại trong khu vực này.");
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
                    throw new ResourceConflictException("Chi nhánh cần ít nhất một khu vực và một kệ đang hoạt động trước khi kích hoạt.");
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
                throw new ResourceConflictException("Đây là khu vực hoạt động cuối cùng có kệ hoạt động trong chi nhánh.");
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

    public Task DeleteBranchAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var branch = await RequireBranchAsync(id, ct);
            EnsureConcurrency(concurrencyToken, branch.ConcurrencyToken);
            if (await repository.BranchHasAreasAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa chi nhánh khi vẫn còn khu vực.");
            if (await repository.BranchHasReferencesAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa chi nhánh vì vẫn còn dữ liệu nghiệp vụ tham chiếu.");
            var before = JsonSerializer.Serialize(branch);
            repository.RemoveBranch(branch);
            AddDeleteAudit("location.branch.deleted", id, before);
            return true;
        }, cancellationToken);

    public Task DeleteAreaAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var area = await RequireAreaAsync(id, ct);
            EnsureConcurrency(concurrencyToken, area.ConcurrencyToken);
            if (await repository.AreaHasShelvesAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa khu vực khi vẫn còn kệ.");
            if (await repository.AreaHasReferencesAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa khu vực vì vẫn còn dữ liệu kiểm kê tham chiếu.");
            var before = JsonSerializer.Serialize(area);
            repository.RemoveArea(area);
            AddDeleteAudit("location.area.deleted", id, before);
            return true;
        }, cancellationToken);

    public Task DeleteShelfAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var shelf = await RequireShelfAsync(id, ct);
            EnsureConcurrency(concurrencyToken, shelf.ConcurrencyToken);
            if (await repository.ShelfHasBookCopiesAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa kệ khi vẫn còn bản sao sách.");
            if (await repository.ShelfHasReferencesAsync(id, ct))
                throw new ResourceConflictException("Không thể xóa kệ vì vẫn còn dữ liệu kiểm kê tham chiếu.");
            var before = JsonSerializer.Serialize(shelf);
            repository.RemoveShelf(shelf);
            AddDeleteAudit("location.shelf.deleted", id, before);
            return true;
        }, cancellationToken);

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
            throw new ResourceConflictException("Không thể kích hoạt kệ khi khu vực đang ngừng hoạt động.");
        if (!command.IsActive)
        {
            EnsureNoImpact(await repository.GetShelfImpactAsync(id, cancellationToken));
            var branch = await RequireBranchAsync(area.BranchId, cancellationToken);
            var readiness = await repository.GetBranchReadinessAsync(area.BranchId, null, id, cancellationToken);
            if (branch.IsActive && !readiness.CanActivate)
                throw new ResourceConflictException("Đây là kệ hoạt động cuối cùng trong chi nhánh.");
        }

        var before = JsonSerializer.Serialize(shelf);
        if (command.IsActive) shelf.Activate(); else shelf.Deactivate();
        AddAudit(command.IsActive ? "location.shelf.activated" : "location.shelf.deactivated", id, before, shelf);
        return Map(shelf);
    }

    private async Task<Branch> RequireBranchAsync(Guid id, CancellationToken ct) =>
        await repository.GetBranchAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy chi nhánh.");

    private async Task<Area> RequireAreaAsync(Guid id, CancellationToken ct) =>
        await repository.GetAreaAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy khu vực.");

    private async Task<Shelf> RequireShelfAsync(Guid id, CancellationToken ct) =>
        await repository.GetShelfAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy kệ.");

    private static void EnsureConcurrency(Guid? expected, Guid actual)
    {
        if (expected is null || expected == Guid.Empty)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["concurrencyToken"] = ["Thiếu phiên bản dữ liệu vị trí."] });
        if (expected != actual)
            throw new OptimisticConcurrencyException("Vị trí đã được cập nhật bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
    }

    private static void EnsureNoImpact(LocationImpactSnapshot impact)
    {
        if (!impact.HasBlockingReferences) return;
        throw new ResourceConflictException(
            $"Vị trí vẫn đang được tham chiếu: {impact.EmployeeCount} nhân viên, {impact.BookCopyCount} bản sao sách, " +
            $"{impact.ActiveInventoryAuditCount} đợt kiểm kê đang hoạt động, {impact.EditableStockReceiptCount} phiếu nhập có thể chỉnh sửa.");
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

    private void AddDeleteAudit(string action, Guid entityId, string before) =>
        unitOfWork.AddAuditLog(AuditLog.Create(
            requestContext.UserId,
            action,
            "Location",
            entityId,
            before,
            null,
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
        if (readiness.ActiveAreaCount == 0) missing.Add("Cần ít nhất một khu vực đang hoạt động.");
        if (readiness.ActiveShelfCount == 0) missing.Add("Cần ít nhất một kệ đang hoạt động trong khu vực hoạt động.");
        return new BranchReadinessModel(readiness.CanActivate, readiness.ActiveAreaCount, readiness.ActiveShelfCount, missing);
    }

    private static LocationImpactModel Map(LocationImpactSnapshot impact) =>
        new(impact.EmployeeCount, impact.BookCopyCount, impact.ActiveInventoryAuditCount,
            impact.EditableStockReceiptCount, impact.HasBlockingReferences);
}
