using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.StockReceipts;

public sealed class StockReceiptService(IStockReceiptRepository repository, IUnitOfWork unitOfWork,
    IRequestContext requestContext, TimeProvider timeProvider)
{
    public async Task<PageResult<StockReceiptModel>> GetPageAsync(StockReceiptQuery query, CancellationToken cancellationToken)
    {
        if (query.FromUtc is DateTime from && query.ToUtc is DateTime to && to < from)
            throw Validation("toUtc", "Ngày kết thúc phải sau ngày bắt đầu.");
        var (number, size) = CollectionLimits.NormalizePage(query.PageNumber, query.PageSize);
        var page = await repository.GetPageAsync(query with
        {
            FromUtc = query.FromUtc.HasValue ? AsUtc(query.FromUtc.Value) : null,
            ToUtc = query.ToUtc.HasValue ? AsUtc(query.ToUtc.Value) : null,
            PageNumber = number, PageSize = size
        }, cancellationToken);
        return new PageResult<StockReceiptModel>(page.Items.Select(Map).ToArray(), number, size, page.TotalCount);
    }

    public async Task<StockReceiptModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(id, cancellationToken)) is { } snapshot ? Map(snapshot) : null;

    public Task<StockReceiptModel> CreateAsync(SaveStockReceiptCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            await ValidateAsync(command, [], ct);
            var actorId = requestContext.UserId ?? throw new UnauthorizedAccessException("Không xác định được người tiếp nhận.");
            StockReceipt receipt;
            try
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                receipt = StockReceipt.Create($"PN-{now:yyyyMMdd}-{Guid.NewGuid():N}",
                    command.SupplierId, command.BranchId, actorId, command.ReceivedAtUtc, command.Notes);
            }
            catch (ArgumentException exception) { throw Validation("receipt", exception.Message); }
            if (await repository.ReceiptNumberExistsAsync(receipt.ReceiptNumber, ct))
                throw new ResourceConflictException("Số phiếu nhập đã tồn tại.");
            await repository.AddAsync(receipt, ct);
            foreach (var row in command.Items)
                await repository.AddItemAsync(CreateItem(receipt.Id, row), ct);
            await unitOfWork.SaveChangesAsync(ct);
            var result = Map((await repository.GetByIdAsync(receipt.Id, ct))!);
            Audit("stock-receipt.created", receipt.Id, null, result);
            return result;
        }, cancellationToken);

    public Task<StockReceiptModel> UpdateAsync(Guid id, SaveStockReceiptCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var receipt = await repository.GetTrackedAsync(id, ct)
                ?? throw new ResourceNotFoundException("Không tìm thấy phiếu nhập.");
            if (command.ConcurrencyToken is null || command.ConcurrencyToken == Guid.Empty)
                throw Validation("concurrencyToken", "Thiếu phiên bản phiếu nhập.");
            if (command.ConcurrencyToken != receipt.ConcurrencyToken)
                throw new OptimisticConcurrencyException("Phiếu nhập đã thay đổi. Vui lòng tải lại.");
            if (receipt.Status is not (StockReceiptStatus.Draft or StockReceiptStatus.Received))
                throw new ResourceConflictException("Phiếu đã xác nhận hoặc hủy không thể sửa.");
            var currentItems = await repository.GetTrackedItemsAsync(id, ct);
            await ValidateAsync(command, currentItems, ct);
            var before = await repository.GetByIdAsync(id, ct);
            try { receipt.Update(command.SupplierId, command.BranchId, command.ReceivedAtUtc, command.Notes); }
            catch (InvalidOperationException exception) { throw new ResourceConflictException(exception.Message); }
            catch (ArgumentException exception) { throw Validation("receipt", exception.Message); }

            var currentById = currentItems.ToDictionary(item => item.Id);
            var requestedIds = command.Items.Where(item => item.Id is not null).Select(item => item.Id!.Value).ToHashSet();
            foreach (var existing in currentItems.Where(item => !requestedIds.Contains(item.Id)))
            {
                if (await repository.HasCopyReferencesAsync(existing.Id, ct))
                    throw new ResourceConflictException("Không thể bỏ dòng đã tạo bản sao sách.");
                repository.RemoveItem(existing);
            }
            foreach (var row in command.Items)
            {
                if (row.Id is Guid itemId)
                {
                    var existing = currentById[itemId];
                    if (existing.BookId != row.BookId)
                        throw Validation("items", "Không thể đổi sách của dòng hiện có. Hãy xóa dòng và thêm dòng mới.");
                    try { existing.Update(row.ExpectedQuantity, row.ReceivedQuantity, row.DamagedQuantity, row.UnitCost); }
                    catch (ArgumentException exception) { throw Validation("items", exception.Message); }
                }
                else await repository.AddItemAsync(CreateItem(id, row), ct);
            }
            await unitOfWork.SaveChangesAsync(ct);
            var result = Map((await repository.GetByIdAsync(id, ct))!);
            Audit("stock-receipt.updated", id, before is null ? null : JsonSerializer.Serialize(Map(before)), result);
            return result;
        }, cancellationToken);

    public Task<ConfirmStockReceiptResult> ConfirmAsync(Guid id, ConfirmStockReceiptCommand command,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async ct =>
    {
        var receipt = await repository.GetTrackedAsync(id, ct)
            ?? throw new ResourceNotFoundException("Không tìm thấy phiếu nhập.");
        if (receipt.Status == StockReceiptStatus.Confirmed)
            return await ConfirmationResultAsync(id, ct);
        if (receipt.Status == StockReceiptStatus.Cancelled)
            throw new ResourceConflictException("Phiếu đã hủy không thể xác nhận.");
        if (command.ConcurrencyToken == Guid.Empty || command.ConcurrencyToken != receipt.ConcurrencyToken)
            throw new OptimisticConcurrencyException("Phiếu đã thay đổi. Vui lòng tải lại.");
        var actorId = requestContext.UserId ?? throw new UnauthorizedAccessException("Không xác định được người xác nhận.");
        var items = await repository.GetTrackedItemsAsync(id, ct);
        if (command.Items is null || command.Items.Count != items.Count ||
            command.Items.Select(row => row.StockReceiptItemId).Distinct().Count() != items.Count ||
            command.Items.Any(row => !items.Any(item => item.Id == row.StockReceiptItemId)))
            throw Validation("items", "Phải xác nhận chính xác một lần cho từng dòng phiếu.");
        if (items.Sum(item => (long)item.ReceivedQuantity) > 1000)
            throw Validation("items", "Một lần xác nhận tối đa 1000 bản sao.");
        if ((await repository.GetReceiptCopiesAsync(id, ct)).Count > 0)
            throw new ResourceConflictException("Phiếu đã có bản sao; cần kiểm tra dữ liệu trước khi xác nhận.");

        var copies = new List<(StockReceiptItem Item, ConfirmReceiptCopyCommand Command, string Barcode)>();
        foreach (var item in items)
        {
            var row = command.Items.Single(value => value.StockReceiptItemId == item.Id);
            if (row.Copies is null || row.Copies.Count != item.ReceivedQuantity)
                throw Validation("items", $"Dòng {item.Id} phải có đúng {item.ReceivedQuantity} bản sao.");
            if (row.Copies.Count(copy => copy.Condition == CopyCondition.Damaged) != item.DamagedQuantity)
                throw Validation("items", $"Dòng {item.Id} phải có đúng {item.DamagedQuantity} bản sao hỏng.");
            foreach (var copy in row.Copies)
            {
                if (!Enum.IsDefined(copy.Condition) || copy.Condition == CopyCondition.Lost)
                    throw Validation("items", "Tình trạng bản sao không hợp lệ khi nhập kho.");
                try { copies.Add((item, copy, BookCopy.NormalizeBarcode(copy.Barcode))); }
                catch (ArgumentException exception) { throw Validation("items", exception.Message); }
            }
        }
        if (copies.Select(copy => copy.Barcode).Distinct(StringComparer.Ordinal).Count() != copies.Count ||
            await repository.AnyBarcodeExistsAsync(copies.Select(copy => copy.Barcode).ToArray(), ct))
            throw new ResourceConflictException("Mã vạch trùng trong phiếu hoặc đã tồn tại.");
        var shelfIds = copies.Select(copy => copy.Command.ShelfId).ToHashSet();
        var activeShelfIds = await repository.GetActiveShelfIdsAsync(receipt.BranchId, shelfIds, ct);
        if (shelfIds.Any(shelfId => !activeShelfIds.Contains(shelfId)))
            throw Validation("items", "Mỗi bản sao phải thuộc kệ đang hoạt động trong chi nhánh của phiếu.");

        var before = JsonSerializer.Serialize(Map((await repository.GetByIdAsync(id, ct))!));
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var (item, copy, barcode) in copies)
        {
            var entity = BookCopy.Create(item.BookId, barcode, copy.Condition, copy.ShelfId, item.Id, now);
            if (copy.Condition == CopyCondition.Damaged) entity.ChangeStatus(CopyStatus.Damaged);
            await repository.AddCopyAsync(entity, ct);
        }
        foreach (var item in items)
        {
            if (item.ExpectedQuantity != item.ReceivedQuantity)
                await repository.AddDiscrepancyAsync(DiscrepancyReport.Create(id,
                    item.ReceivedQuantity < item.ExpectedQuantity ? DiscrepancyType.Missing : DiscrepancyType.Excess,
                    item.ExpectedQuantity, item.ReceivedQuantity,
                    $"Dòng {item.Id}: dự kiến {item.ExpectedQuantity}, thực nhận {item.ReceivedQuantity}.", now, actorId), ct);
            if (item.DamagedQuantity > 0)
                await repository.AddDiscrepancyAsync(DiscrepancyReport.Create(id, DiscrepancyType.Damaged,
                    item.ReceivedQuantity, item.ReceivedQuantity - item.DamagedQuantity,
                    $"Dòng {item.Id}: {item.DamagedQuantity} bản sao hỏng trong {item.ReceivedQuantity} bản sao thực nhận.", now, actorId), ct);
        }
        receipt.Confirm(now);
        await unitOfWork.SaveChangesAsync(ct);
        var result = await ConfirmationResultAsync(id, ct);
        Audit("stock-receipt.confirmed", id, before, result.Receipt);
        return result;
    }, cancellationToken);

    public async Task<ConfirmStockReceiptResult?> GetConfirmationAsync(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetByIdAsync(id, cancellationToken);
        return snapshot is null ? null : await ConfirmationResultAsync(id, cancellationToken);
    }

    private async Task<ConfirmStockReceiptResult> ConfirmationResultAsync(Guid id, CancellationToken cancellationToken)
    {
        var receipt = Map((await repository.GetByIdAsync(id, cancellationToken))!);
        var copies = await repository.GetReceiptCopiesAsync(id, cancellationToken);
        var discrepancies = await repository.GetDiscrepanciesAsync(id, cancellationToken);
        return new ConfirmStockReceiptResult(receipt,
            copies.Select(copy => new ReceiptCopyModel(copy.Id, copy.StockReceiptItemId!.Value,
                copy.Barcode, copy.ShelfId!.Value, copy.Condition, copy.Status)).ToArray(),
            discrepancies.Select(report => new DiscrepancyModel(report.Id, report.Type,
                report.ExpectedQuantity, report.ActualQuantity, report.Description,
                report.CreatedAtUtc, report.CreatedByUserId)).ToArray());
    }

    private async Task ValidateAsync(SaveStockReceiptCommand command,
        IReadOnlyList<StockReceiptItem> existingItems, CancellationToken cancellationToken)
    {
        if (command.ReceivedAtUtc == default) throw Validation("receivedAtUtc", "Thời điểm tiếp nhận không hợp lệ.");
        if (!await repository.SupplierActiveAsync(command.SupplierId, cancellationToken))
            throw Validation("supplierId", "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động.");
        if (!await repository.BranchActiveAsync(command.BranchId, cancellationToken))
            throw Validation("branchId", "Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
        if (command.Items is null || command.Items.Count is < 1 or > 100)
            throw Validation("items", "Phiếu phải có từ 1 đến 100 dòng.");
        var bookIds = command.Items.Select(item => item.BookId).ToArray();
        if (bookIds.Any(id => id == Guid.Empty) || bookIds.Distinct().Count() != bookIds.Length)
            throw Validation("items", "Mỗi dòng phải có một sách khác nhau.");
        var existingById = existingItems.ToDictionary(item => item.Id);
        var requestedIds = command.Items.Where(item => item.Id is not null).Select(item => item.Id!.Value).ToArray();
        if (requestedIds.Length != requestedIds.Distinct().Count() || requestedIds.Any(id => !existingById.ContainsKey(id)))
            throw Validation("items", "Mã dòng phiếu không hợp lệ.");
        var activeBooks = await repository.GetActiveBookIdsAsync(bookIds, cancellationToken);
        foreach (var row in command.Items)
        {
            if (!activeBooks.Contains(row.BookId) &&
                !(row.Id is Guid id && existingById[id].BookId == row.BookId))
                throw Validation("items", "Dòng phiếu tham chiếu sách không hoạt động.");
            try { _ = StockReceiptItem.Create(Guid.NewGuid(), row.BookId,
                row.ExpectedQuantity, row.ReceivedQuantity, row.DamagedQuantity, row.UnitCost); }
            catch (ArgumentException exception) { throw Validation("items", exception.Message); }
        }
    }

    private static StockReceiptItem CreateItem(Guid receiptId, SaveStockReceiptItemCommand row)
    {
        try { return StockReceiptItem.Create(receiptId, row.BookId, row.ExpectedQuantity,
            row.ReceivedQuantity, row.DamagedQuantity, row.UnitCost); }
        catch (ArgumentException exception) { throw Validation("items", exception.Message); }
    }

    private void Audit(string action, Guid id, string? before, StockReceiptModel after) =>
        unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, action, nameof(StockReceipt), id,
            before, JsonSerializer.Serialize(after), timeProvider.GetUtcNow().UtcDateTime,
            requestContext.CorrelationId, requestContext.IpAddress));

    private static RequestValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
    private static StockReceiptModel Map(StockReceiptSnapshot snapshot)
    {
        var receipt = snapshot.Receipt;
        var items = snapshot.Items.Select(row => new StockReceiptItemModel(row.Item.Id, row.Item.BookId,
            row.BookTitle, row.Isbn, row.Item.ExpectedQuantity, row.Item.ReceivedQuantity,
            row.Item.DamagedQuantity, row.Item.UnitCost, (row.Item.UnitCost ?? 0) * row.Item.ReceivedQuantity,
            row.Item.ConcurrencyToken)).ToArray();
        return new StockReceiptModel(receipt.Id, receipt.ReceiptNumber, receipt.SupplierId, snapshot.SupplierName,
            receipt.BranchId, snapshot.BranchCode, receipt.ReceivedByUserId, receipt.Status,
            receipt.ReceivedAtUtc, receipt.Notes, receipt.ConcurrencyToken,
            items.Sum(row => (long)row.ReceivedQuantity), items.Sum(row => row.TotalValue), items);
    }
}
