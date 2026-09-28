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
    private static readonly string[] RequiredHeaders = ["title", "author", "isbn", "category"];
    private static readonly string[] ExportHeaders =
        ["Title", "Author", "ISBN", "Category", "Publisher", "Description", "EditionStatement", "PublicationYear", "Language", "PageCount"];
    private static readonly IReadOnlyDictionary<string, string> HeaderAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["title"] = "title", ["tensach"] = "title",
        ["author"] = "author", ["tacgia"] = "author",
        ["isbn"] = "isbn",
        ["category"] = "category", ["theloai"] = "category",
        ["publisher"] = "publisher", ["nhaxuatban"] = "publisher",
        ["description"] = "description", ["mota"] = "description",
        ["editionstatement"] = "editionstatement", ["edition"] = "editionstatement", ["anban"] = "editionstatement",
        ["publicationyear"] = "publicationyear", ["namxuatban"] = "publicationyear",
        ["language"] = "language", ["ngonngu"] = "language",
        ["pagecount"] = "pagecount", ["sotrang"] = "pagecount"
    };

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
                query.Status,
                sortBy,
                query.SortDirection == SortDirection.Desc,
                cancellationToken)).Items;
        var catalogs = catalogRepository is null
            ? new Dictionary<Guid, BookCatalogSnapshot>()
            : await catalogRepository.GetCatalogAsync(books.Select(book => book.Id).ToArray(), cancellationToken);
        var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true))
        {
            await writer.WriteLineAsync(string.Join(',', ExportHeaders));
            foreach (var book in books)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(string.Join(',',
                    Csv(book.Title), Csv(book.Author), Csv(book.Isbn), Csv(book.Category),
                    Csv(catalogs.TryGetValue(book.Id, out var catalog) ? catalog.Publisher?.Name ?? string.Empty : string.Empty),
                    Csv(book.Description ?? string.Empty), Csv(book.EditionStatement ?? string.Empty),
                    book.PublicationYear?.ToString() ?? string.Empty, Csv(book.Language ?? string.Empty),
                    book.PageCount?.ToString() ?? string.Empty));
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

        if (records.Count == 0)
            return new ImportPreview<BookImportRow>([], [new(1, "header", "Tệp CSV không có dòng tiêu đề.")], Checksum([]));

        var headerErrors = new List<ImportFieldError>();
        var headerIndexes = BuildHeaderIndexes(records[0], headerErrors);
        if (headerErrors.Count > 0)
            return new ImportPreview<BookImportRow>([], headerErrors, Checksum([]));

        if (records.Count - 1 > CollectionLimits.MaximumImportRows)
            return new ImportPreview<BookImportRow>([], [new(1, "file", $"Tệp không được vượt quá {CollectionLimits.MaximumImportRows} dòng dữ liệu.")], Checksum([]));

        var rows = new List<BookImportRow>();
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            var rowNumber = index + 1;
            if (record.Count == 1 && string.IsNullOrWhiteSpace(record[0])) continue;
            if (record.Count > records[0].Count)
            {
                errors.Add(new(rowNumber, "row", "Dòng dữ liệu có nhiều cột hơn dòng tiêu đề."));
                continue;
            }
            var title = Value(record, headerIndexes, "title");
            var author = Value(record, headerIndexes, "author");
            var isbn = Value(record, headerIndexes, "isbn");
            var category = Value(record, headerIndexes, "category");
            var publisher = OptionalValue(record, headerIndexes, "publisher");
            var description = OptionalValue(record, headerIndexes, "description");
            var edition = OptionalValue(record, headerIndexes, "editionstatement");
            var language = OptionalValue(record, headerIndexes, "language");
            var publicationYear = ParseOptionalInteger(record, headerIndexes, "publicationyear", rowNumber, 0, 9999, errors);
            var pageCount = ParseOptionalInteger(record, headerIndexes, "pagecount", rowNumber, 1, 100000, errors);
            ValidateRequired(title, rowNumber, "title", 200, errors);
            ValidateRequired(author, rowNumber, "author", 200, errors);
            ValidateRequired(isbn, rowNumber, "isbn", 32, errors);
            try { _ = IsbnValue.Create(isbn); }
            catch (ArgumentException) { errors.Add(new(rowNumber, "isbn", "ISBN-10 hoặc ISBN-13 không hợp lệ.")); }
            ValidateRequired(category, rowNumber, "category", 100, errors);
            ValidateOptional(publisher, rowNumber, "publisher", 200, errors);
            ValidateOptional(description, rowNumber, "description", 4000, errors);
            ValidateOptional(edition, rowNumber, "editionStatement", 200, errors);
            ValidateOptional(language, rowNumber, "language", 100, errors);
            rows.Add(new(rowNumber, title.Trim(), author.Trim(), NormalizeIsbn(isbn), category.Trim(), publisher,
                description, edition, publicationYear, language, pageCount));
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
            ValidateOptional(row.Publisher, row.RowNumber, "publisher", 200, validationErrors);
            ValidateOptional(row.Description, row.RowNumber, "description", 4000, validationErrors);
            ValidateOptional(row.EditionStatement, row.RowNumber, "editionStatement", 200, validationErrors);
            ValidateOptional(row.Language, row.RowNumber, "language", 100, validationErrors);
            if (row.PublicationYear is < 0 or > 9999)
                validationErrors.Add(new(row.RowNumber, "publicationYear", "Năm xuất bản phải từ 0 đến 9999."));
            if (row.PageCount is < 1 or > 100000)
                validationErrors.Add(new(row.RowNumber, "pageCount", "Số trang phải từ 1 đến 100000."));
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
                var book = Book.Create(row.Title, row.Author, row.Isbn, row.Category, now);
                book.SetPublicationMetadata(null, row.EditionStatement, row.Description,
                    row.PublicationYear, row.Language, row.PageCount);
                await repository.AddAsync(book, ct);
                if (catalogRepository is not null)
                    await catalogRepository.NormalizeBookAsync(
                        book, row.Author, row.Category, row.Publisher, ct);
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
            if (catalogRepository is not null && await catalogRepository.HasActiveDependenciesAsync(id, cancellationToken))
            {
                results.Add(new(id, false, "Biểu ghi còn bản sao, lượt mượn hoặc đặt trước đang hoạt động."));
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
        "author" or "isbn" or "category" or "createdAtUtc" => value,
        _ => "title"
    };

    private static string Csv(string value)
    {
        var safe = value.Length > 0 && "=+-@".Contains(value[0]) ? $"'{value}" : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().TrimStart('\uFEFF').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
    private static string NormalizeIsbn(string value) => value.Trim().Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
    private static void ValidateRequired(string value, int row, string field, int maximum, ICollection<ImportFieldError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(row, field, "Không được để trống."));
        else if (value.Trim().Length > maximum) errors.Add(new(row, field, $"Không được vượt quá {maximum} ký tự."));
    }

    private static void ValidateOptional(string? value, int row, string field, int maximum, ICollection<ImportFieldError> errors)
    {
        if (value?.Trim().Length > maximum)
            errors.Add(new(row, field, $"Không được vượt quá {maximum} ký tự."));
    }

    private static string Checksum(IEnumerable<BookImportRow> rows)
    {
        var canonical = JsonSerializer.Serialize(rows.Select(row => new
        {
            row.RowNumber, row.Title, row.Author, row.Isbn, row.Category, row.Publisher,
            row.Description, row.EditionStatement, row.PublicationYear, row.Language, row.PageCount
        }));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static Dictionary<string, int> BuildHeaderIndexes(IReadOnlyList<string> headers, ICollection<ImportFieldError> errors)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < headers.Count; index++)
        {
            var normalized = NormalizeHeader(headers[index]);
            if (!HeaderAliases.TryGetValue(normalized, out var canonical))
            {
                errors.Add(new(1, "header", $"Cột '{headers[index].Trim()}' không được hỗ trợ."));
                continue;
            }
            if (!result.TryAdd(canonical, index))
                errors.Add(new(1, "header", $"Cột '{headers[index].Trim()}' bị lặp."));
        }
        foreach (var required in RequiredHeaders.Where(required => !result.ContainsKey(required)))
            errors.Add(new(1, "header", $"Thiếu cột bắt buộc '{required}'."));
        return result;
    }

    private static string Value(IReadOnlyList<string> record, IReadOnlyDictionary<string, int> indexes, string name) =>
        indexes.TryGetValue(name, out var index) && index < record.Count ? record[index] : string.Empty;

    private static string? OptionalValue(IReadOnlyList<string> record, IReadOnlyDictionary<string, int> indexes, string name)
    {
        var value = Value(record, indexes, name).Trim();
        return value.Length == 0 ? null : value;
    }

    private static int? ParseOptionalInteger(
        IReadOnlyList<string> record, IReadOnlyDictionary<string, int> indexes, string name,
        int rowNumber, int minimum, int maximum, ICollection<ImportFieldError> errors)
    {
        var value = Value(record, indexes, name).Trim();
        if (value.Length == 0) return null;
        if (int.TryParse(value, out var parsed) && parsed >= minimum && parsed <= maximum) return parsed;
        errors.Add(new(rowNumber, name, $"Giá trị phải là số nguyên từ {minimum} đến {maximum}."));
        return null;
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
