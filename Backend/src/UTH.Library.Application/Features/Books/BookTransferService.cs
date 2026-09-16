using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.ValueObjects;

namespace UTH.Library.Application.Features.Books;

public sealed class BookTransferService(
    IBookRepository repository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    TimeProvider timeProvider)
{
    private static readonly string[] Header = ["title", "author", "isbn", "category", "quantity"];

    public async Task<Stream> ExportAsync(BookListQuery query, CancellationToken cancellationToken)
    {
        var sortBy = NormalizeSort(query.SortBy);
        var books = catalogRepository is null
            ? await repository.GetForExportAsync(
                query.Search,
                query.Category,
                sortBy,
                query.SortDirection == SortDirection.Desc,
                CollectionLimits.MaximumExportRows,
                cancellationToken)
            : (await catalogRepository.GetPageAsync(
                query.Search,
                query.Category,
                1,
                CollectionLimits.MaximumExportRows,
                query.AuthorIds,
                query.CategoryIds,
                query.PublisherId,
                sortBy,
                query.SortDirection == SortDirection.Desc,
                cancellationToken)).Items;
        var catalogs = catalogRepository is null
            ? new Dictionary<Guid, BookCatalogSnapshot>()
            : await catalogRepository.GetCatalogAsync(books.Select(book => book.Id).ToArray(), cancellationToken);
        var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true))
        {
            await writer.WriteLineAsync("Title,Author,ISBN,Category,Quantity");
            foreach (var book in books)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(string.Join(',',
                    Csv(book.Title), Csv(book.Author), Csv(book.Isbn), Csv(book.Category),
                    catalogs.TryGetValue(book.Id, out var catalog) ? catalog.AvailableCopyCount : book.Quantity));
            }
        }
        stream.Position = 0;
        return stream;
    }

    public async Task<ImportPreview<BookImportRow>> PreviewImportAsync(
        byte[] content,
        CancellationToken cancellationToken)
    {
        var errors = new List<ImportFieldError>();
        IReadOnlyList<IReadOnlyList<string>> records;
        try { records = ParseCsv(new UTF8Encoding(false, true).GetString(content)); }
        catch (Exception exception) when (exception is DecoderFallbackException or FormatException)
        {
            return new ImportPreview<BookImportRow>([], [new(1, "file", exception.Message)], Checksum([]));
        }

        if (records.Count == 0 || !Header.SequenceEqual(records[0].Select(NormalizeHeader)))
            return new ImportPreview<BookImportRow>([], [new(1, "header", "Tiêu đề CSV phải là Title, Author, ISBN, Category, Quantity.")], Checksum([]));

        if (records.Count - 1 > CollectionLimits.MaximumImportRows)
            return new ImportPreview<BookImportRow>([], [new(1, "file", $"Tệp không được vượt quá {CollectionLimits.MaximumImportRows} dòng dữ liệu.")], Checksum([]));

        var rows = new List<BookImportRow>();
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            var rowNumber = index + 1;
            if (record.Count == 1 && string.IsNullOrWhiteSpace(record[0])) continue;
            if (record.Count != Header.Length)
            {
                errors.Add(new(rowNumber, "row", $"Cần đúng {Header.Length} cột."));
                continue;
            }
            if (!int.TryParse(record[4].Trim(), out var quantity) || quantity is < 0 or > 100_000)
                errors.Add(new(rowNumber, "quantity", "Số lượng phải là số nguyên từ 0 đến 100000."));
            ValidateRequired(record[0], rowNumber, "title", 200, errors);
            ValidateRequired(record[1], rowNumber, "author", 200, errors);
            ValidateRequired(record[2], rowNumber, "isbn", 32, errors);
            try { _ = IsbnValue.Create(record[2]); }
            catch (ArgumentException) { errors.Add(new(rowNumber, "isbn", "ISBN-10 hoặc ISBN-13 không hợp lệ.")); }
            ValidateRequired(record[3], rowNumber, "category", 100, errors);
            rows.Add(new(rowNumber, record[0].Trim(), record[1].Trim(), NormalizeIsbn(record[2]), record[3].Trim(), quantity));
        }

        var duplicateIsbns = rows.GroupBy(row => row.Isbn, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var existingIsbns = await repository.GetExistingIsbnsAsync(rows.Select(row => row.Isbn), cancellationToken);
        foreach (var row in rows)
        {
            if (duplicateIsbns.Contains(row.Isbn)) errors.Add(new(row.RowNumber, "isbn", "ISBN bị trùng trong tệp."));
            else if (existingIsbns.Contains(row.Isbn)) errors.Add(new(row.RowNumber, "isbn", "ISBN đã tồn tại trong hệ thống."));
        }

        return new ImportPreview<BookImportRow>(rows, errors, Checksum(rows));
    }

    public async Task<BookImportResult> ConfirmImportAsync(
        ConfirmBookImportCommand command,
        CancellationToken cancellationToken)
    {
        var suppliedChecksum = Encoding.UTF8.GetBytes(command.Checksum);
        var expectedChecksum = Encoding.UTF8.GetBytes(Checksum(command.Rows));
        if (suppliedChecksum.Length != expectedChecksum.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedChecksum, expectedChecksum))
            return new(0, [new(1, "checksum", "Dữ liệu xác nhận không khớp với bản xem trước.")], requestContext.CorrelationId);

        var validationErrors = new List<ImportFieldError>();
        foreach (var row in command.Rows)
        {
            ValidateRequired(row.Title, row.RowNumber, "title", 200, validationErrors);
            ValidateRequired(row.Author, row.RowNumber, "author", 200, validationErrors);
            ValidateRequired(row.Category, row.RowNumber, "category", 100, validationErrors);
            if (row.Quantity is < 0 or > 100_000)
                validationErrors.Add(new(row.RowNumber, "quantity", "Số lượng phải là số nguyên từ 0 đến 100000."));
            try { _ = IsbnValue.Create(row.Isbn); }
            catch (ArgumentException) { validationErrors.Add(new(row.RowNumber, "isbn", "ISBN-10 hoặc ISBN-13 không hợp lệ.")); }
        }
        if (validationErrors.Count > 0)
            return new(0, validationErrors, requestContext.CorrelationId);

        var normalizedRows = command.Rows
            .Select(row => (Row: row, Isbn: IsbnValue.Create(row.Isbn).Value))
            .ToArray();
        var duplicateIsbns = normalizedRows.Select(item => item.Isbn).GroupBy(isbn => isbn, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var existingIsbns = await repository.GetExistingIsbnsAsync(normalizedRows.Select(item => item.Isbn), cancellationToken);
        var errors = normalizedRows
            .Where(item => duplicateIsbns.Contains(item.Isbn) || existingIsbns.Contains(item.Isbn))
            .Select(item => new ImportFieldError(item.Row.RowNumber, "isbn", "ISBN bị trùng hoặc đã được thêm sau khi xem trước."))
            .ToArray();
        if (errors.Length > 0) return new(0, errors, requestContext.CorrelationId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            foreach (var row in command.Rows)
            {
                var book = Book.Create(row.Title, row.Author, row.Isbn, row.Category, row.Quantity, now);
                await repository.AddAsync(book, ct);
                if (catalogRepository is not null)
                    await catalogRepository.NormalizeImportedBookAsync(
                        book, row.Author, row.Category, row.Quantity, now, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.imported", nameof(Book), book.Id,
                    null, JsonSerializer.Serialize(book), now, requestContext.CorrelationId));
            }
            return new BookImportResult(command.Rows.Count, [], requestContext.CorrelationId);
        }, cancellationToken);
    }

    public async Task<BulkResult> DeleteBulkAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().Take(CollectionLimits.MaximumBulkItems).ToArray();
        var results = new List<BulkItemResult>(distinctIds.Length);
        foreach (var id in distinctIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var book = await repository.GetByIdAsync(id, cancellationToken);
            if (book is null)
            {
                results.Add(new(id, false, "Không tìm thấy sách."));
                continue;
            }
            try
            {
                await unitOfWork.ExecuteAsync(ct =>
                {
                    var before = JsonSerializer.Serialize(book);
                    book.Deactivate(timeProvider.GetUtcNow().UtcDateTime);
                    unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.deactivated", nameof(Book), book.Id,
                        before, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
                    return Task.FromResult(true);
                }, cancellationToken);
                results.Add(new(id, true));
            }
            catch (Exception exception) when (exception is ResourceConflictException or InvalidOperationException)
            {
                results.Add(new(id, false, exception.Message));
            }
        }
        return new BulkResult(results, requestContext.CorrelationId);
    }

    public static string NormalizeSort(string? value) => value switch
    {
        "author" or "isbn" or "category" or "quantity" or "createdAtUtc" => value,
        _ => "title"
    };

    private static string Csv(string value)
    {
        var safe = value.Length > 0 && "=+-@".Contains(value[0]) ? $"'{value}" : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static string NormalizeHeader(string value) => value.Trim().TrimStart('\uFEFF').ToLowerInvariant();
    private static string NormalizeIsbn(string value) => value.Trim().Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
    private static void ValidateRequired(string value, int row, string field, int maximum, ICollection<ImportFieldError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(row, field, "Không được để trống."));
        else if (value.Trim().Length > maximum) errors.Add(new(row, field, $"Không được vượt quá {maximum} ký tự."));
    }

    private static string Checksum(IEnumerable<BookImportRow> rows)
    {
        var canonical = JsonSerializer.Serialize(rows.Select(row => new { row.RowNumber, row.Title, row.Author, row.Isbn, row.Category, row.Quantity }));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static IReadOnlyList<IReadOnlyList<string>> ParseCsv(string text)
    {
        var records = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"') quoted = true;
            else if (character == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(field.ToString()); field.Clear(); records.Add(row); row = [];
            }
            else field.Append(character);
        }
        if (quoted) throw new FormatException("Dấu ngoặc kép trong CSV không hợp lệ.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row); }
        return records;
    }

    private IBookCatalogRepository? catalogRepository => repository as IBookCatalogRepository;
}
