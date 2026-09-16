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
        var (pageNumber, pageSize) = CollectionLimits.NormalizePage(query.PageNumber, query.PageSize);
        var sortBy = BookTransferService.NormalizeSort(query.SortBy);
        var (items, totalCount) = catalogRepository is null
            ? await repository.GetPageAsync(query.Search, query.Category, pageNumber, pageSize, query.SortBy, query.SortDirection == SortDirection.Desc, cancellationToken)
            : await catalogRepository.GetPageAsync(
                query.Search,
                query.Category,
                pageNumber,
                pageSize,
                query.AuthorIds,
                query.CategoryIds,
                query.PublisherId,
                query.Status,
                sortBy,
                query.SortDirection == SortDirection.Desc,
                cancellationToken);

        var catalogs = catalogRepository is null
            ? new Dictionary<Guid, BookCatalogSnapshot>()
            : await catalogRepository.GetCatalogAsync(items.Select(item => item.Id).ToArray(), cancellationToken);
        return new BookPageModel(
            items.Select(item => Map(item, catalogs.GetValueOrDefault(item.Id))).ToArray(),
            pageNumber,
            pageSize,
            totalCount);
    }

    Task<BookPageModel> IQueryHandler<BookListQuery, BookPageModel>.HandleAsync(BookListQuery query, CancellationToken cancellationToken) => GetAsync(query, cancellationToken);
    Task<BookResult> ICommandHandler<CreateBookCommand, BookResult>.HandleAsync(CreateBookCommand command, CancellationToken cancellationToken) => CreateAsync(command, cancellationToken);

    public async Task<BookModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await repository.GetByIdAsync(id, cancellationToken);
        return book is null
            ? null
            : Map(book, catalogRepository is null
                ? null
                : await catalogRepository.GetCatalogAsync(id, cancellationToken));
    }

    public Task<IReadOnlyList<BookCatalogReference>> SearchReferencesAsync(
        string type,
        string? search,
        CancellationToken cancellationToken) =>
        catalogRepository is null
            ? Task.FromResult<IReadOnlyList<BookCatalogReference>>([])
            : catalogRepository.SearchReferencesAsync(type, search, 30, cancellationToken);

    public async Task<BookResult> CreateAsync(CreateBookCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return BookResult.Fail(BookFailure.Validation, errors.ToArray());
        if (!await ReferencesAreValidAsync(command.AuthorIds, command.CategoryIds, command.PublisherId, cancellationToken))
            return BookResult.Fail(BookFailure.Validation, "One or more catalog references are invalid or inactive.");
        try
        {
            var book = Book.Create(
                command.Title,
                command.Author,
                command.Isbn,
                command.Category,
                command.Quantity,
                timeProvider.GetUtcNow().UtcDateTime);
            book.SetPublicationMetadata(command.PublisherId, command.EditionStatement, command.Description,
                command.PublicationYear, command.Language, command.PageCount);

            if (await repository.IsbnExistsAsync(book.Isbn, null, cancellationToken))
                return BookResult.Fail(BookFailure.Conflict, "A book with this ISBN already exists.");

            return await unitOfWork.ExecuteAsync(async ct =>
            {
                await repository.AddAsync(book, ct);
                if (catalogRepository is not null && HasNormalizedReferences(command))
                    await catalogRepository.ReplaceRelationshipsAsync(
                        book.Id,
                        command.AuthorIds ?? [],
                        command.CategoryIds ?? [],
                        command.PublisherId,
                        ct);
                else if (catalogRepository is not null)
                    await catalogRepository.NormalizeBookAsync(book, command.Author, command.Category, command.PublisherName, ct);
                if (catalogRepository is not null)
                    await catalogRepository.SetAvailableCopyCountAsync(book.Id, command.Quantity, book.CreatedAtUtc, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.created", nameof(Book), book.Id, null, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
                return BookResult.Success(Map(
                    book,
                    catalogRepository is null ? null : await catalogRepository.GetCatalogAsync(book.Id, ct)));
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
        if (!await ReferencesAreValidAsync(command.AuthorIds, command.CategoryIds, command.PublisherId, cancellationToken))
            return BookResult.Fail(BookFailure.Validation, "One or more catalog references are invalid or inactive.");
        var book = await repository.GetByIdAsync(id, cancellationToken);
        if (book is null)
            return BookResult.Fail(BookFailure.NotFound, "Book was not found.");
        if (command.ConcurrencyToken is not null && command.ConcurrencyToken != book.ConcurrencyToken)
            return BookResult.Fail(BookFailure.Conflict, "Biểu ghi đã thay đổi. Vui lòng tải lại trước khi lưu.");

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
            book.SetPublicationMetadata(command.PublisherId, command.EditionStatement, command.Description,
                command.PublicationYear, command.Language, command.PageCount);
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                if (catalogRepository is not null && HasNormalizedReferences(command))
                    await catalogRepository.ReplaceRelationshipsAsync(
                        book.Id,
                        command.AuthorIds ?? [],
                        command.CategoryIds ?? [],
                        command.PublisherId,
                        ct);
                else if (catalogRepository is not null)
                    await catalogRepository.NormalizeBookAsync(book, command.Author, command.Category, command.PublisherName, ct);
                if (catalogRepository is not null)
                    await catalogRepository.SetAvailableCopyCountAsync(book.Id, command.Quantity, timeProvider.GetUtcNow().UtcDateTime, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.updated", nameof(Book), book.Id, before, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
                return BookResult.Success(Map(
                    book,
                    catalogRepository is null ? null : await catalogRepository.GetCatalogAsync(book.Id, ct)));
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
        if (catalogRepository is not null && await catalogRepository.HasActiveDependenciesAsync(id, cancellationToken))
            return BookResult.Fail(BookFailure.Conflict, "Không thể ngừng sử dụng biểu ghi khi còn bản sao, lượt mượn hoặc đặt trước đang hoạt động.");

        return await unitOfWork.ExecuteAsync(ct =>
        {
            var before = JsonSerializer.Serialize(book);
            book.Deactivate(timeProvider.GetUtcNow().UtcDateTime);
            unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, "book.deactivated", nameof(Book), book.Id, before, JsonSerializer.Serialize(book), timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
            return Task.FromResult(BookResult.Success(Map(book)));
        }, cancellationToken);
    }

    private async Task<bool> ReferencesAreValidAsync(
        IReadOnlyCollection<Guid>? authorIds,
        IReadOnlyCollection<Guid>? categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken) =>
        catalogRepository is null ||
        await catalogRepository.ReferencesExistAsync(
            authorIds ?? [], categoryIds ?? [], publisherId, cancellationToken);

    private static bool HasNormalizedReferences(CreateBookCommand command) =>
        command.AuthorIds is not null || command.CategoryIds is not null || command.PublisherId is not null;

    private static bool HasNormalizedReferences(UpdateBookCommand command) =>
        command.AuthorIds is not null || command.CategoryIds is not null || command.PublisherId is not null;

    private static BookModel Map(Book book, BookCatalogSnapshot? catalog = null) =>
        new(
            book.Id,
            book.Title,
            book.Author,
            book.Isbn,
            book.Category,
            catalog?.AvailableCopyCount ?? book.Quantity,
            book.CreatedAtUtc,
            book.UpdatedAtUtc,
            catalog?.Authors.Select(reference => new BookReferenceModel(reference.Id, reference.Name)).ToArray(),
            catalog?.Categories.Select(reference => new BookReferenceModel(reference.Id, reference.Name)).ToArray(),
            catalog?.Publisher is null ? null : new BookReferenceModel(catalog.Publisher.Id, catalog.Publisher.Name),
            catalog?.AvailableCopyCount,
            book.Status,
            book.ConcurrencyToken,
            book.Description,
            book.EditionStatement,
            book.PublicationYear,
            book.Language,
            book.PageCount);

    private IBookCatalogRepository? catalogRepository => repository as IBookCatalogRepository;

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
