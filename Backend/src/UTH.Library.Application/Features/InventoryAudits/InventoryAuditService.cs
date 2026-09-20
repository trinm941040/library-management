using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.InventoryAudits;

public sealed class InventoryAuditService(IInventoryAuditRepository repository, IUnitOfWork unitOfWork,
    IRequestContext requestContext, TimeProvider timeProvider)
{
    public async Task<PageResult<InventoryAuditModel>> GetPageAsync(InventoryAuditQuery query,
        CancellationToken cancellationToken)
    {
        var (page, size) = CollectionLimits.NormalizePage(query.PageNumber, query.PageSize);
        var result = await repository.GetPageAsync(query with { PageNumber = page, PageSize = size }, cancellationToken);
        var models = new List<InventoryAuditModel>(result.Items.Count);
        foreach (var audit in result.Items)
            models.Add(Map((await repository.GetSnapshotAsync(audit.Id, cancellationToken))!));
        return new PageResult<InventoryAuditModel>(models, page, size, result.TotalCount);
    }

    public async Task<InventoryAuditModel?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        (await repository.GetSnapshotAsync(id, cancellationToken)) is { } snapshot ? Map(snapshot) : null;

    public Task<InventoryAuditModel> CreateAsync(CreateInventoryAuditCommand command,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async ct =>
    {
        var actor = requestContext.UserId ?? throw new UnauthorizedAccessException("Không xác định được người kiểm kê.");
        if (command.BranchId == Guid.Empty || command.AreaId == Guid.Empty || command.ShelfId == Guid.Empty ||
            (command.ShelfId is not null && command.AreaId is null) ||
            !await repository.ScopeActiveAsync(command.BranchId, command.AreaId, command.ShelfId, ct))
            throw Validation("scope", "Chi nhánh, khu vực hoặc kệ không hợp lệ hoặc đã ngừng hoạt động.");
        InventoryAudit audit;
        try { audit = InventoryAudit.Create(command.BranchId, command.AreaId, command.ShelfId,
            actor, command.Notes, timeProvider.GetUtcNow().UtcDateTime); }
        catch (ArgumentException exception) { throw Validation("scope", exception.Message); }
        var copies = await repository.GetExpectedCopiesAsync(command.BranchId,
            command.AreaId, command.ShelfId, ct);
        await repository.AddAsync(audit, ct);
        await repository.AddItemsAsync(copies.Select(copy => InventoryAuditItem.CreateExpected(audit.Id, copy)).ToArray(), ct);
        await unitOfWork.SaveChangesAsync(ct);
        var result = Map((await repository.GetSnapshotAsync(audit.Id, ct))!);
        Audit("inventory-audit.started", audit.Id, null, result);
        return result;
    }, cancellationToken);

    public Task<InventoryAuditModel> ScanAsync(Guid id, ScanInventoryAuditCommand command,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async ct =>
    {
        var audit = await RequireOpenAsync(id, command.ConcurrencyToken, ct);
        if (string.IsNullOrWhiteSpace(command.Barcode) || command.ActualShelfId == Guid.Empty ||
            !Enum.IsDefined(command.ActualStatus) || !Enum.IsDefined(command.ActualCondition) ||
            !await repository.ShelfExistsAsync(command.ActualShelfId, ct))
            throw Validation("scan", "Mã vạch, kệ thực tế hoặc tình trạng không hợp lệ.");
        BookCopy? copy;
        try { copy = await repository.GetCopyByBarcodeAsync(BookCopy.NormalizeBarcode(command.Barcode), ct); }
        catch (ArgumentException exception) { throw Validation("barcode", exception.Message); }
        if (copy is null) throw new ResourceNotFoundException("Không tìm thấy mã vạch.");
        var item = await repository.GetTrackedItemAsync(id, copy.Id, ct);
        if (item is not null && item.Result != AuditItemResult.Pending)
            throw new ResourceConflictException("Mã vạch đã được quét trong đợt này.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            if (item is null)
                await repository.AddItemAsync(InventoryAuditItem.CreateUnexpected(id, copy,
                    command.ActualShelfId, command.ActualStatus, command.ActualCondition, now), ct);
            else item.Scan(command.ActualShelfId, command.ActualStatus, command.ActualCondition, now);
        }
        catch (ArgumentException exception) { throw Validation("scan", exception.Message); }
        audit.Touch();
        await unitOfWork.SaveChangesAsync(ct);
        return Map((await repository.GetSnapshotAsync(id, ct))!);
    }, cancellationToken);

    public async Task<InventoryAuditModel?> ReconcileAsync(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetSnapshotAsync(id, cancellationToken);
        return snapshot is null ? null : Map(snapshot, previewMissing: true);
    }

    public Task<InventoryAuditModel> CompleteAsync(Guid id, CompleteInventoryAuditCommand command,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async ct =>
    {
        var audit = await repository.GetTrackedAsync(id, ct)
            ?? throw new ResourceNotFoundException("Không tìm thấy đợt kiểm kê.");
        if (audit.Status == InventoryAuditStatus.Completed)
            return Map((await repository.GetSnapshotAsync(id, ct))!);
        if (audit.Status != InventoryAuditStatus.InProgress)
            throw new ResourceConflictException("Đợt kiểm kê không còn hoạt động.");
        if (command.ConcurrencyToken == Guid.Empty || audit.ConcurrencyToken != command.ConcurrencyToken)
            throw new OptimisticConcurrencyException("Đợt kiểm kê đã thay đổi. Vui lòng tải lại.");
        var items = await repository.GetTrackedItemsAsync(id, ct);
        var hasDiscrepancy = items.Any(item => item.Result != AuditItemResult.Found);
        if (hasDiscrepancy && !command.AcknowledgeDiscrepancies)
            throw new ResourceConflictException("Cần xác nhận chủ đích xử lý các chênh lệch trước khi hoàn tất.");
        var before = JsonSerializer.Serialize(Map((await repository.GetSnapshotAsync(id, ct))!));
        foreach (var item in items.Where(item => item.Result == AuditItemResult.Pending)) item.MarkMissing();
        audit.Complete(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        var result = Map((await repository.GetSnapshotAsync(id, ct))!);
        Audit("inventory-audit.completed", id, before, result);
        return result;
    }, cancellationToken);

    public Task<int> ApplyCorrectionsAsync(Guid id, ApplyInventoryCorrectionsCommand command,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async ct =>
    {
        var audit = await repository.GetTrackedAsync(id, ct)
            ?? throw new ResourceNotFoundException("Không tìm thấy đợt kiểm kê.");
        if (audit.Status != InventoryAuditStatus.Completed)
            throw new ResourceConflictException("Chỉ áp dụng chênh lệch sau khi hoàn tất kiểm kê.");
        if (command.Corrections is null || command.Corrections.Count is < 1 or > 100 ||
            command.Corrections.Select(row => row.BookCopyId).Distinct().Count() != command.Corrections.Count)
            throw Validation("corrections", "Chọn từ 1 đến 100 bản sao không trùng nhau.");
        var discrepantIds = (await repository.GetTrackedItemsAsync(id, ct))
            .Where(item => item.Result is not AuditItemResult.Found).Select(item => item.BookCopyId).ToHashSet();
        var before = new List<object>();
        var after = new List<object>();
        foreach (var row in command.Corrections)
        {
            if (!discrepantIds.Contains(row.BookCopyId) ||
                (row.ShelfId is null && row.Status is null && row.Condition is null))
                throw Validation("corrections", "Chỉ có thể xử lý bản sao chênh lệch với thay đổi cụ thể.");
            var copy = await repository.GetTrackedCopyAsync(row.BookCopyId, ct)
                ?? throw new ResourceNotFoundException("Không tìm thấy bản sao sách.");
            if (row.ConcurrencyToken == Guid.Empty || copy.ConcurrencyToken != row.ConcurrencyToken)
                throw new OptimisticConcurrencyException("Bản sao đã thay đổi. Vui lòng tải lại.");
            if (row.ShelfId is Guid shelfId && !await repository.ShelfExistsAsync(shelfId, ct))
                throw Validation("corrections", "Kệ đích không tồn tại.");
            before.Add(new { copy.Id, copy.Barcode, copy.ShelfId, copy.Status, copy.Condition });
            try
            {
                if (row.Status is CopyStatus status) copy.ChangeStatus(status);
                if (row.ShelfId is Guid destination) copy.Relocate(destination);
                if (row.Condition is CopyCondition condition) copy.ChangeCondition(condition);
            }
            catch (InvalidOperationException exception) { throw new ResourceConflictException(exception.Message); }
            catch (ArgumentException exception) { throw Validation("corrections", exception.Message); }
            after.Add(new { copy.Id, copy.Barcode, copy.ShelfId, copy.Status, copy.Condition });
        }
        unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "inventory-audit.discrepancy-applied",
            nameof(InventoryAudit), id, JsonSerializer.Serialize(before), JsonSerializer.Serialize(after),
            timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId, requestContext.IpAddress));
        return command.Corrections.Count;
    }, cancellationToken);

    private async Task<InventoryAudit> RequireOpenAsync(Guid id, Guid token, CancellationToken ct)
    {
        var audit = await repository.GetTrackedAsync(id, ct)
            ?? throw new ResourceNotFoundException("Không tìm thấy đợt kiểm kê.");
        if (audit.Status != InventoryAuditStatus.InProgress)
            throw new ResourceConflictException("Đợt kiểm kê đã khóa.");
        if (token == Guid.Empty || audit.ConcurrencyToken != token)
            throw new OptimisticConcurrencyException("Đợt kiểm kê đã thay đổi. Vui lòng tải lại.");
        return audit;
    }

    private void Audit(string action, Guid id, string? before, InventoryAuditModel after) =>
        unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, action,
            nameof(InventoryAudit), id, before, JsonSerializer.Serialize(after),
            timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId, requestContext.IpAddress));

    private static InventoryAuditModel Map(InventoryAuditSnapshot snapshot, bool previewMissing = false)
    {
        var audit = snapshot.Audit;
        var items = snapshot.Items.Select(value =>
        {
            var item = value.Item;
            return new InventoryAuditItemModel(item.Id, item.BookCopyId, value.CopyConcurrencyToken,
                value.Barcode, value.BookTitle,
                item.IsExpected, item.ExpectedShelfId, item.ActualShelfId,
                item.ExpectedStatus, item.ActualStatus, item.ExpectedCondition, item.ActualCondition,
                previewMissing && item.Result == AuditItemResult.Pending ? AuditItemResult.Missing : item.Result,
                item.ScannedAtUtc);
        }).ToArray();
        return new InventoryAuditModel(audit.Id, audit.BranchId, audit.AreaId, audit.ShelfId,
            audit.StartedByUserId, audit.Status, audit.StartedAtUtc, audit.CompletedAtUtc,
            audit.Notes, audit.ConcurrencyToken, items.Count(item => item.IsExpected),
            items.Count(item => item.ScannedAtUtc is not null),
            snapshot.Items.Count(item => item.Item.Result == AuditItemResult.Pending),
            items.Count(item => item.Result == AuditItemResult.Found),
            items.Count(item => item.Result is not (AuditItemResult.Pending or AuditItemResult.Found)), items);
    }

    private static RequestValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
