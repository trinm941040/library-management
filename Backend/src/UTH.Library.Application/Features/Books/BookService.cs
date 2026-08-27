using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Books;

public sealed class BookService(IBookRepository repository, TimeProvider timeProvider)
{
    public async Task<BookPageModel> GetAsync(BookListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, totalCount) = await repository.GetPageAsync(
            query.Search,
            query.Category,
            pageNumber,
            pageSize,
            cancellationToken);

        return new BookPageModel(items.Select(Map).ToArray(), pageNumber, pageSize, totalCount);
    }

    public async Task<BookModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : Map(book);
    }

    public async Task<BookResult> CreateAsync(CreateBookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var book = Book.Create(
                command.Title,
                command.Author,
                command.Isbn,
                command.Category,
                command.Quantity,
                timeProvider.GetUtcNow().UtcDateTime);

            if (await repository.IsbnExistsAsync(book.Isbn, null, cancellationToken))
                return BookResult.Fail(BookFailure.Conflict, "A book with this ISBN already exists.");

            await repository.AddAsync(book, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return BookResult.Success(Map(book));
        }
        catch (ArgumentException exception)
        {
            return BookResult.Fail(BookFailure.Validation, exception.Message);
        }
    }

    public async Task<BookResult> UpdateAsync(Guid id, UpdateBookCommand command, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
            return BookResult.Fail(BookFailure.NotFound, "Book was not found.");

        try
        {
            var isbn = command.Isbn.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
            if (await repository.IsbnExistsAsync(isbn, id, cancellationToken))
                return BookResult.Fail(BookFailure.Conflict, "A book with this ISBN already exists.");

            book.Update(
                command.Title,
                command.Author,
                command.Isbn,
                command.Category,
                command.Quantity,
                timeProvider.GetUtcNow().UtcDateTime);
            await repository.SaveChangesAsync(cancellationToken);
            return BookResult.Success(Map(book));
        }
        catch (ArgumentException exception)
        {
            return BookResult.Fail(BookFailure.Validation, exception.Message);
        }
    }

    public async Task<BookResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
            return BookResult.Fail(BookFailure.NotFound, "Book was not found.");

        repository.Remove(book);
        await repository.SaveChangesAsync(cancellationToken);
        return BookResult.Success(Map(book));
    }

    private static BookModel Map(Book book) =>
        new(book.Id, book.Title, book.Author, book.Isbn, book.Category, book.Quantity, book.CreatedAtUtc, book.UpdatedAtUtc);
}
