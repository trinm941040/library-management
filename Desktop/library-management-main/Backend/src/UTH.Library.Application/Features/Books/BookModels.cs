using UTH.Library.Application.Common;
using UTH.Library.Domain.Enums;
namespace UTH.Library.Application.Features.Books;

public sealed record BookModel(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<BookReferenceModel>? Authors = null,
    IReadOnlyCollection<BookReferenceModel>? Categories = null,
    BookReferenceModel? Publisher = null,
    int? AvailableCopyCount = null,
    RecordStatus Status = RecordStatus.Active,
    Guid ConcurrencyToken = default,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null);

public sealed record BookReferenceModel(Guid Id, string Name);

public sealed record BookListQuery(
    string? Search,
    string? Category,
    int PageNumber,
    int PageSize,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    RecordStatus? Status = null,
    string SortBy = "title",
    string SortDirection = "asc") : IQuery<BookPageModel>;

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
    int Quantity,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null) : ICommand<BookResult>;

public sealed record UpdateBookCommand(
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null,
    Guid? ConcurrencyToken = null);

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
