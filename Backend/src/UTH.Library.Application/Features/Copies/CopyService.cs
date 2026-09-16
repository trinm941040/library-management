using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Copies;

public sealed class CopyService(IBookCopyRepository repository, IUnitOfWork unitOfWork,
    IRequestContext requestContext, TimeProvider timeProvider)
{
    public async Task<PageResult<CopyModel>> GetPageAsync(BookCopyQuery query, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = CollectionLimits.NormalizePage(query.PageNumber, query.PageSize);
        var page = await repository.GetPageAsync(query with { PageNumber = pageNumber, PageSize = pageSize }, cancellationToken);
        return new PageResult<CopyModel>(page.Items.Select(Map).ToArray(), page.PageNumber, page.PageSize, page.TotalCount);
    }

    public async Task<CopyModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        MapOrNull(await repository.GetByIdAsync(id, cancellationToken));

    public async Task<CopyModel?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        MapOrNull(await repository.GetByBarcodeAsync(NormalizeBarcode(barcode), cancellationToken));

    public async Task<CopyModel> CreateAsync(CreateCopyCommand command, CancellationToken cancellationToken)
    {
        var barcode = NormalizeBarcode(command.Barcode);
        if (!Enum.IsDefined(command.Condition)) throw Validation("condition", "Tình trạng bản sao không hợp lệ.");
        if (!await repository.ActiveBookExistsAsync(command.BookId, cancellationToken))
            throw Validation("bookId", "Biểu ghi không tồn tại hoặc đã ngừng hoạt động.");
        if (!await repository.ActiveShelfExistsAsync(command.ShelfId, cancellationToken))
            throw Validation("shelfId", "Kệ hoặc chi nhánh đã ngừng hoạt động.");
        if (command.StockReceiptItemId is Guid receiptId &&
            !await repository.ReceiptItemMatchesBookAsync(receiptId, command.BookId, cancellationToken))
            throw Validation("stockReceiptItemId", "Dòng phiếu nhập không thuộc biểu ghi hoặc phiếu chưa được xác nhận.");
        if (await repository.BarcodeExistsAsync(barcode, cancellationToken))
            throw new ResourceConflictException("Mã vạch đã tồn tại.");

        var copy = BookCopy.Create(command.BookId, barcode, command.Condition, command.ShelfId,
            command.StockReceiptItemId, UtcNow());
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddAsync(copy, ct);
            Audit("book-copy.created", copy.Id, null, copy);
            await unitOfWork.SaveChangesAsync(ct);
            return MapOrNull(await repository.GetByIdAsync(copy.Id, ct))!;
        }, cancellationToken);
    }

    public Task<CopyModel> ChangeStatusAsync(Guid id, ChangeCopyStatusCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var copy = await repository.GetTrackedAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy bản sao.");
            EnsureToken(command.ConcurrencyToken, copy.ConcurrencyToken);
            if (copy.Status == command.Status)
                return MapOrNull(await repository.GetByIdAsync(id, ct))!;
            if (await repository.HasActiveAuditAsync(id, ct))
                throw new ResourceConflictException("Bản sao đang được kiểm kê.");
            if (await repository.HasEditableReceiptAsync(copy.StockReceiptItemId, ct))
                throw new ResourceConflictException("Phiếu nhập liên kết chưa được xác nhận.");
            if (copy.Status == CopyStatus.Borrowed || command.Status == CopyStatus.Borrowed)
                throw new ResourceConflictException("Trạng thái mượn phải được thay đổi qua nghiệp vụ mượn/trả.");
            if (await repository.HasActiveBorrowingForBookAsync(copy.BookId, ct) &&
                command.Status is CopyStatus.Withdrawn or CopyStatus.Lost)
                throw new ResourceConflictException("Biểu ghi đang có khoản mượn; không thể mất/thanh lý bản sao khi chưa đối soát.");
            var before = JsonSerializer.Serialize(copy);
            try { copy.ChangeStatus(command.Status); }
            catch (InvalidOperationException exception) { throw new ResourceConflictException(exception.Message); }
            Audit("book-copy.status-changed", id, before, copy);
            await unitOfWork.SaveChangesAsync(ct);
            return MapOrNull(await repository.GetByIdAsync(id, ct))!;
        }, cancellationToken);

    public Task<CopyModel> RelocateAsync(Guid id, RelocateCopyCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var copy = await repository.GetTrackedAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy bản sao.");
            EnsureToken(command.ConcurrencyToken, copy.ConcurrencyToken);
            if (!await repository.ActiveShelfExistsAsync(command.ShelfId, ct))
                throw Validation("shelfId", "Kệ hoặc chi nhánh đã ngừng hoạt động.");
            if (await repository.HasActiveAuditAsync(id, ct))
                throw new ResourceConflictException("Bản sao đang được kiểm kê.");
            if (await repository.HasEditableReceiptAsync(copy.StockReceiptItemId, ct))
                throw new ResourceConflictException("Phiếu nhập liên kết chưa được xác nhận.");
            var before = JsonSerializer.Serialize(copy);
            try { copy.Relocate(command.ShelfId); }
            catch (InvalidOperationException exception) { throw new ResourceConflictException(exception.Message); }
            if (before != JsonSerializer.Serialize(copy)) Audit("book-copy.relocated", id, before, copy);
            await unitOfWork.SaveChangesAsync(ct);
            return MapOrNull(await repository.GetByIdAsync(id, ct))!;
        }, cancellationToken);

    private void Audit(string action, Guid id, string? before, BookCopy copy) =>
        unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, action, nameof(BookCopy), id,
            before, JsonSerializer.Serialize(copy), UtcNow(), requestContext.CorrelationId, requestContext.IpAddress));

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
    private static void EnsureToken(Guid expected, Guid actual)
    {
        if (expected == Guid.Empty) throw Validation("concurrencyToken", "Thiếu phiên bản bản sao.");
        if (expected != actual) throw new OptimisticConcurrencyException("Bản sao đã được sửa. Vui lòng tải lại.");
    }
    private static RequestValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
    private static string NormalizeBarcode(string barcode)
    {
        try { return BookCopy.NormalizeBarcode(barcode); }
        catch (ArgumentException exception) { throw Validation("barcode", exception.Message); }
    }
    private static CopyModel? MapOrNull(BookCopySnapshot? snapshot) => snapshot is null ? null : Map(snapshot);
    private static CopyModel Map(BookCopySnapshot snapshot)
    {
        var copy = snapshot.Copy;
        return new CopyModel(copy.Id, copy.BookId, snapshot.BookTitle, copy.Barcode, copy.Condition,
            copy.Status, copy.AcquiredAtUtc, copy.ShelfId, snapshot.ShelfCode, snapshot.BranchId,
            snapshot.BranchCode, copy.StockReceiptItemId, copy.ConcurrencyToken);
    }
}
