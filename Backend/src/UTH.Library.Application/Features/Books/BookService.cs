using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Application.Abstractions;
using System.Text.Json;
using UTH.Library.Application.Common;

namespace UTH.Library.Application.Features.Books;

public sealed class BookService(IBookRepository repository, IUnitOfWork unitOfWork, IRequestContext requestContext, BookCommandValidator validator, TimeProvider timeProvider)
    : IQueryHandler<BookListQuery, BookPageModel>, ICommandHandler<CreateBookCommand, BookResult>
{
    public BookService(IBookRepository repository, TimeProvider timeProvider)
        : this(repository, new RepositoryUnitOfWork(repository), EmptyRequestContext.Instance, new BookCommandValidator(), timeProvider) { }

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

    Task<BookPageModel> IQueryHandler<BookListQuery, BookPageModel>.HandleAsync(BookListQuery query, CancellationToken cancellationToken) => GetAsync(query, cancellationToken);
    Task<BookResult> ICommandHandler<CreateBookCommand, BookResult>.HandleAsync(CreateBookCommand command, CancellationToken cancellationToken) => CreateAsync(command, cancellationToken);

    public async Task<BookModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : Map(book);
    }

    public async Task<BookResult> CreateAsync(CreateBookCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return BookResult.Fail(BookFailure.Validation, errors.ToArray());
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

            return await unitOfWork.ExecuteAsync(async ct =>
            {
                await repository.AddAsync(book, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.created", nameof(Book), book.Id, null, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
                return BookResult.Success(Map(book));
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BookResult.Fail(BookFailure.Validation, exception.Message);
        }
    }

    public async Task<BookResult> UpdateAsync(Guid id, UpdateBookCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return BookResult.Fail(BookFailure.Validation, errors.ToArray());
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
            return BookResult.Fail(BookFailure.NotFound, "Book was not found.");

        try
        {
            var isbn = command.Isbn.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
            if (await repository.IsbnExistsAsync(isbn, id, cancellationToken))
                return BookResult.Fail(BookFailure.Conflict, "A book with this ISBN already exists.");

            var before = JsonSerializer.Serialize(book);
            book.Update(
                command.Title,
                command.Author,
                command.Isbn,
                command.Category,
                command.Quantity,
                timeProvider.GetUtcNow().UtcDateTime);
            return await unitOfWork.ExecuteAsync(ct =>
            {
                unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.updated", nameof(Book), book.Id, before, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
                return Task.FromResult(BookResult.Success(Map(book)));
            }, cancellationToken);
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

        return await unitOfWork.ExecuteAsync(ct =>
        {
            repository.Remove(book);
            unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.deleted", nameof(Book), book.Id, JsonSerializer.Serialize(book), null, timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
            return Task.FromResult(BookResult.Success(Map(book)));
        }, cancellationToken);
    }

    private static BookModel Map(Book book) =>
        new(book.Id, book.Title, book.Author, book.Isbn, book.Category, book.Quantity, book.CreatedAtUtc, book.UpdatedAtUtc);

    private sealed class RepositoryUnitOfWork(IBookRepository repository) : IUnitOfWork
    {
        public void AddAuditLog(AuditLog auditLog) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Save(cancellationToken);
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        { var result = await operation(cancellationToken); await repository.SaveChangesAsync(cancellationToken); return result; }
        private async Task<int> Save(CancellationToken cancellationToken) { await repository.SaveChangesAsync(cancellationToken); return 0; }
    }
    private sealed class EmptyRequestContext : IRequestContext
    {
        public static readonly EmptyRequestContext Instance = new();
        public Guid? UserId => null;
        public string CorrelationId => string.Empty;
        public string? IpAddress => null;
    }
}
