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
    int PageSize) : IQuery<BookPageModel>;

public sealed record BookPageModel(
    IReadOnlyList<BookModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

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
