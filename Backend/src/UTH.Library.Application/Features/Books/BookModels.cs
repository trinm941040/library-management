using UTH.Library.Application.Common;
namespace UTH.Library.Application.Features.Books;

public sealed record BookModel(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record BookListQuery(
    string? Search,
    string? Category,
    int PageNumber,
    int PageSize,
    string SortBy = "title",
    SortDirection SortDirection = SortDirection.Asc) : IQuery<BookPageModel>;

public sealed record BookPageModel(
    IReadOnlyList<BookModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record BookImportRow(
    int RowNumber,
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity);

public sealed record ConfirmBookImportCommand(
    IReadOnlyList<BookImportRow> Rows,
    string Checksum);

public sealed record BookImportResult(
    int ImportedCount,
    IReadOnlyList<ImportFieldError> Errors,
    string CorrelationId);

public sealed record CreateBookCommand(
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity) : ICommand<BookResult>;

public sealed record UpdateBookCommand(
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity);

public enum BookFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record BookResult(bool Succeeded, BookFailure Failure, BookModel? Book, IReadOnlyList<string> Errors)
{
    public static BookResult Success(BookModel book) => new(true, BookFailure.None, book, []);

    public static BookResult Fail(BookFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
