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
