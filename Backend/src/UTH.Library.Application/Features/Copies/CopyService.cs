using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Domain.ValueObjects;

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

    public async Task<IReadOnlyList<CopyModel>> GetExportAsync(BookCopyQuery query, CancellationToken cancellationToken) =>
        (await repository.GetExportAsync(query, cancellationToken)).Select(Map).ToArray();

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
            if (command.Status == CopyStatus.Withdrawn)
                throw new ResourceConflictException("Thanh lý phải qua thao tác có lý do và quyền quản lý.");
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

    public Task<CopyModel> ChangeConditionAsync(Guid id, ChangeCopyConditionCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var copy = await repository.GetTrackedAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy bản sao.");
            EnsureToken(command.ConcurrencyToken, copy.ConcurrencyToken);
            if (!Enum.IsDefined(command.Condition)) throw Validation("condition", "Tình trạng bản sao không hợp lệ.");
            if (copy.Status is CopyStatus.Borrowed or CopyStatus.Withdrawn || await repository.HasActiveAuditAsync(id, ct))
                throw new ResourceConflictException("Bản sao đang mượn, đã thanh lý hoặc đang kiểm kê.");
            var before = JsonSerializer.Serialize(copy);
            copy.ChangeCondition(command.Condition);
            if (before != JsonSerializer.Serialize(copy)) Audit("book-copy.condition-changed", id, before, copy);
            await unitOfWork.SaveChangesAsync(ct);
            return MapOrNull(await repository.GetByIdAsync(id, ct))!;
        }, cancellationToken);

    public Task<CopyModel> WithdrawAsync(Guid id, WithdrawCopyCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 500)
                throw Validation("reason", "Lý do thanh lý phải có từ 1 đến 500 ký tự.");
            var copy = await repository.GetTrackedAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy bản sao.");
            EnsureToken(command.ConcurrencyToken, copy.ConcurrencyToken);
            if (copy.Status == CopyStatus.Withdrawn)
                throw new ResourceConflictException("Bản sao đã được thanh lý.");
            if (await repository.HasActiveAuditAsync(id, ct) || await repository.HasEditableReceiptAsync(copy.StockReceiptItemId, ct)
                || await repository.HasActiveBorrowingForBookAsync(copy.BookId, ct))
                throw new ResourceConflictException("Bản sao còn liên quan kiểm kê, phiếu nhập hoặc khoản mượn đang hoạt động.");
            var before = JsonSerializer.Serialize(copy);
            try { copy.Withdraw(); }
            catch (InvalidOperationException exception) { throw new ResourceConflictException(exception.Message); }
            unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book-copy.withdrawn", nameof(BookCopy), id,
                before, JsonSerializer.Serialize(new { Copy = copy, Reason = command.Reason.Trim() }), UtcNow(),
                requestContext.CorrelationId, requestContext.IpAddress));
            await unitOfWork.SaveChangesAsync(ct);
            return MapOrNull(await repository.GetByIdAsync(id, ct))!;
        }, cancellationToken);

    public async Task<IReadOnlyList<CopyOperationResult>> ApplyBulkAsync(IReadOnlyList<CopyOperationRow> rows,
        string operation, CancellationToken cancellationToken)
    {
        if (rows.Count is < 1 or > 100) throw Validation("rows", "Mỗi lần xử lý từ 1 đến 100 bản sao.");
        var seen = new HashSet<Guid>();
        var results = new List<CopyOperationResult>(rows.Count);
        foreach (var row in rows)
        {
            if (!seen.Add(row.CopyId)) { results.Add(new(row.CopyId, false, "Bản sao bị lặp trong yêu cầu.", null)); continue; }
            try
            {
                CopyModel copy = operation switch
                {
                    "relocate" when row.ShelfId is Guid shelf => await RelocateAsync(row.CopyId, new(shelf, row.ConcurrencyToken), cancellationToken),
                    "status" when row.Status is CopyStatus status => await ChangeStatusAsync(row.CopyId, new(status, row.ConcurrencyToken), cancellationToken),
                    "condition" when row.Condition is CopyCondition condition => await ChangeConditionAsync(row.CopyId, new(condition, row.ConcurrencyToken), cancellationToken),
                    "withdraw" => await WithdrawAsync(row.CopyId, new(row.ConcurrencyToken, row.Reason ?? ""), cancellationToken),
                    _ => throw Validation("rows", "Thiếu thông tin thao tác cho bản sao.")
                };
                results.Add(new(row.CopyId, true, null, copy));
            }
            catch (Exception exception) when (exception is RequestValidationException or ResourceConflictException
                or ResourceNotFoundException or OptimisticConcurrencyException or ArgumentException)
            {
                repository.DiscardPendingChanges();
                results.Add(new(row.CopyId, false, exception.Message, null));
            }
        }
        return results;
    }

    public async Task<IReadOnlyList<ImportCopyPreviewRow>> PreviewImportAsync(IReadOnlyList<ImportCopyRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count is < 1 or > 1000) throw Validation("rows", "Tệp phải có từ 1 đến 1000 dòng.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var preview = new List<ImportCopyPreviewRow>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            string? error = null;
            string barcode;
            try { barcode = BookCopy.NormalizeBarcode(row.Barcode); }
            catch (ArgumentException exception) { barcode = row.Barcode?.Trim() ?? ""; error = exception.Message; }
            if (error is null && !seen.Add(barcode)) error = "Mã vạch trùng trong tệp.";
            if (error is null && await repository.BarcodeExistsAsync(barcode, cancellationToken)) error = "Mã vạch đã tồn tại.";
            if (error is null && !Enum.IsDefined(row.Condition)) error = "Tình trạng không hợp lệ.";
            if (error is null)
            {
                var resolved = await ResolveImportReferencesAsync(row, cancellationToken);
                error = resolved.Error;
            }
            preview.Add(new(index + 1, barcode, error is null, error));
        }
        return preview;
    }

    public Task<IReadOnlyList<CopyModel>> ConfirmImportAsync(IReadOnlyList<ImportCopyRow> rows, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<CopyModel>>(async ct =>
        {
            var preview = await PreviewImportAsync(rows, ct);
            if (preview.Any(row => !row.Valid))
                throw new ResourceConflictException("Tệp đã thay đổi hoặc có dòng không hợp lệ; xem trước lại trước khi xác nhận.");
            var created = new List<CopyModel>(rows.Count);
            foreach (var row in rows)
            {
                var resolved = await ResolveImportReferencesAsync(row, ct);
                if (resolved.Error is not null || resolved.BookId is null || resolved.ShelfId is null)
                    throw new ResourceConflictException(resolved.Error ?? "Không thể xác định sách hoặc kệ từ dữ liệu nhập.");
                created.Add(await CreateAsync(new(resolved.BookId.Value, row.Barcode, row.Condition, resolved.ShelfId.Value, null), ct));
            }
            return created;
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
    private async Task<(Guid? BookId, Guid? ShelfId, string? Error)> ResolveImportReferencesAsync(
        ImportCopyRow row, CancellationToken cancellationToken)
    {
        string isbn;
        try { isbn = IsbnValue.Create(row.Isbn).Value; }
        catch (ArgumentException) { return (null, null, "ISBN-10 hoặc ISBN-13 không hợp lệ."); }
        var shelfCode = row.ShelfCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (shelfCode.Length == 0) return (null, null, "Mã kệ là bắt buộc.");

        var bookIds = await repository.GetActiveBookIdsByIsbnAsync(isbn, cancellationToken);
        if (bookIds.Count == 0) return (null, null, "Không tìm thấy biểu ghi sách đang hoạt động theo ISBN.");
        if (bookIds.Count > 1) return (null, null, "ISBN khớp nhiều biểu ghi sách; cần xử lý dữ liệu trùng trước khi nhập.");

        var shelfIds = await repository.GetActiveShelfIdsByCodeAsync(shelfCode, cancellationToken);
        if (shelfIds.Count == 0) return (null, null, "Không tìm thấy kệ đang hoạt động theo mã kệ.");
        if (shelfIds.Count > 1) return (null, null, "Mã kệ khớp nhiều kệ; vui lòng dùng mã kệ duy nhất.");
        return (bookIds[0], shelfIds[0], null);
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
