using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class BookRepository(LibraryDbContext dbContext) : IBookRepository, IBookCatalogRepository
{
    public Task AddAsync(Book book, CancellationToken cancellationToken) =>
        dbContext.Books.AddAsync(book, cancellationToken).AsTask();

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Books.SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

    public Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId, CancellationToken cancellationToken) =>
        dbContext.Books.AnyAsync(
            book => book.Isbn == isbn && (excludeId == null || book.Id != excludeId),
            cancellationToken);

    public async Task<BookCatalogSnapshot> GetCatalogAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var snapshot = await LoadCatalogAsync([bookId], cancellationToken);
        return snapshot.TryGetValue(bookId, out var result)
            ? result
            : new BookCatalogSnapshot([], [], null, 0);
    }

    public Task<IReadOnlyDictionary<Guid, BookCatalogSnapshot>> GetCatalogAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken) => LoadCatalogAsync(bookIds, cancellationToken);

    public async Task<bool> ReferencesExistAsync(
        IReadOnlyCollection<Guid> authorIds,
        IReadOnlyCollection<Guid> categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken)
    {
        var distinctAuthorIds = authorIds.Distinct().ToArray();
        var distinctCategoryIds = categoryIds.Distinct().ToArray();
        var authorsValid = distinctAuthorIds.Length == 0 || await dbContext.Authors
            .CountAsync(author => distinctAuthorIds.Contains(author.Id) && author.Status == RecordStatus.Active, cancellationToken) == distinctAuthorIds.Length;
        var categoriesValid = distinctCategoryIds.Length == 0 || await dbContext.Categories
            .CountAsync(category => distinctCategoryIds.Contains(category.Id) && category.Status == RecordStatus.Active, cancellationToken) == distinctCategoryIds.Length;
        var publisherValid = publisherId is null || await dbContext.Publishers
            .AnyAsync(publisher => publisher.Id == publisherId && publisher.Status == RecordStatus.Active, cancellationToken);
        return authorsValid && categoriesValid && publisherValid;
    }

    public async Task ReplaceRelationshipsAsync(
        Guid bookId,
        IReadOnlyCollection<Guid> authorIds,
        IReadOnlyCollection<Guid> categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken)
    {
        var book = dbContext.Books.Local.SingleOrDefault(item => item.Id == bookId)
            ?? await dbContext.Books.SingleAsync(item => item.Id == bookId, cancellationToken);
        var existingAuthors = await dbContext.BookAuthors
            .Where(link => link.BookId == bookId)
            .ToListAsync(cancellationToken);
        var existingCategories = await dbContext.BookCategories
            .Where(link => link.BookId == bookId)
            .ToListAsync(cancellationToken);
        dbContext.BookAuthors.RemoveRange(existingAuthors);
        dbContext.BookCategories.RemoveRange(existingCategories);

        await dbContext.BookAuthors.AddRangeAsync(
            authorIds.Distinct().Select(authorId => BookAuthor.Create(bookId, authorId)),
            cancellationToken);
        await dbContext.BookCategories.AddRangeAsync(
            categoryIds.Distinct().Select(categoryId => BookCategory.Create(bookId, categoryId)),
            cancellationToken);
        book.SetPublicationMetadata(
            publisherId,
            book.EditionStatement,
            book.Description,
            book.PublicationYear,
            book.Language,
            book.PageCount);
    }

    public async Task NormalizeImportedBookAsync(
        Book book,
        string authorName,
        string categoryName,
        CancellationToken cancellationToken)
    {
        var normalizedAuthorName = authorName.Trim();
        var author = dbContext.Authors.Local.FirstOrDefault(
            item => string.Equals(item.FullName, normalizedAuthorName, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.Authors.FirstOrDefaultAsync(
                item => EF.Functions.ILike(item.FullName, normalizedAuthorName), cancellationToken);
        if (author is null)
        {
            author = Author.Create(normalizedAuthorName);
            await dbContext.Authors.AddAsync(author, cancellationToken);
        }

        var normalizedCategoryName = categoryName.Trim();
        var category = dbContext.Categories.Local.FirstOrDefault(
            item => string.Equals(item.Name, normalizedCategoryName, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.Categories.FirstOrDefaultAsync(
                item => EF.Functions.ILike(item.Name, normalizedCategoryName), cancellationToken);
        if (category is null)
        {
            category = Category.Create(normalizedCategoryName);
            await dbContext.Categories.AddAsync(category, cancellationToken);
        }

        await dbContext.BookAuthors.AddAsync(BookAuthor.Create(book.Id, author.Id), cancellationToken);
        await dbContext.BookCategories.AddAsync(BookCategory.Create(book.Id, category.Id), cancellationToken);
    }

    public async Task NormalizeBookAsync(
        Book book,
        string authorName,
        string categoryName,
        string? publisherName,
        CancellationToken cancellationToken)
    {
        var normalizedAuthorName = authorName.Trim();
        var author = dbContext.Authors.Local.FirstOrDefault(item =>
            string.Equals(item.FullName, normalizedAuthorName, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.Authors.FirstOrDefaultAsync(item =>
                EF.Functions.ILike(item.FullName, normalizedAuthorName), cancellationToken);
        if (author is null)
        {
            author = Author.Create(normalizedAuthorName);
            await dbContext.Authors.AddAsync(author, cancellationToken);
        }

        var normalizedCategoryName = categoryName.Trim();
        var category = dbContext.Categories.Local.FirstOrDefault(item =>
            string.Equals(item.Name, normalizedCategoryName, StringComparison.OrdinalIgnoreCase))
            ?? await dbContext.Categories.FirstOrDefaultAsync(item =>
                EF.Functions.ILike(item.Name, normalizedCategoryName), cancellationToken);
        if (category is null)
        {
            category = Category.Create(normalizedCategoryName);
            await dbContext.Categories.AddAsync(category, cancellationToken);
        }

        Guid? publisherId = null;
        if (!string.IsNullOrWhiteSpace(publisherName))
        {
            var normalizedPublisherName = publisherName.Trim();
            var publisher = dbContext.Publishers.Local.FirstOrDefault(item =>
                string.Equals(item.Name, normalizedPublisherName, StringComparison.OrdinalIgnoreCase))
                ?? await dbContext.Publishers.FirstOrDefaultAsync(item =>
                    EF.Functions.ILike(item.Name, normalizedPublisherName), cancellationToken);
            if (publisher is null)
            {
                publisher = Publisher.Create(normalizedPublisherName);
                await dbContext.Publishers.AddAsync(publisher, cancellationToken);
            }
            publisherId = publisher.Id;
        }

        await ReplaceRelationshipsAsync(book.Id, [author.Id], [category.Id], publisherId, cancellationToken);
    }

    public async Task<bool> HasActiveDependenciesAsync(Guid bookId, CancellationToken cancellationToken) =>
        await dbContext.BookCopies.AnyAsync(copy => copy.BookId == bookId && copy.Status != CopyStatus.Withdrawn, cancellationToken) ||
        await dbContext.Borrowings.AnyAsync(item => item.BookId == bookId && item.ReturnedAtUtc == null, cancellationToken) ||
        await dbContext.Reservations.AnyAsync(item => item.BookId == bookId && item.FulfilledAtUtc == null && item.CancelledAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<BookCatalogReference>> SearchReferencesAsync(
        string type,
        string? search,
        int maximumResults,
        CancellationToken cancellationToken)
    {
        var keyword = search?.Trim();
        return type.Trim().ToLowerInvariant() switch
        {
            "authors" => await dbContext.Authors.AsNoTracking()
                .Where(item => item.Status == RecordStatus.Active &&
                    (keyword == null || EF.Functions.ILike(item.FullName, $"%{keyword}%")))
                .OrderBy(item => item.FullName).Take(maximumResults)
                .Select(item => new BookCatalogReference(item.Id, item.FullName)).ToListAsync(cancellationToken),
            "publishers" => await dbContext.Publishers.AsNoTracking()
                .Where(item => item.Status == RecordStatus.Active &&
                    (keyword == null || EF.Functions.ILike(item.Name, $"%{keyword}%")))
                .OrderBy(item => item.Name).Take(maximumResults)
                .Select(item => new BookCatalogReference(item.Id, item.Name)).ToListAsync(cancellationToken),
            "categories" => await dbContext.Categories.AsNoTracking()
                .Where(item => item.Status == RecordStatus.Active &&
                    (keyword == null || EF.Functions.ILike(item.Name, $"%{keyword}%")))
                .OrderBy(item => item.Name).Take(maximumResults)
                .Select(item => new BookCatalogReference(item.Id, item.Name)).ToListAsync(cancellationToken),
            _ => []
        };
    }

    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = Filter(search, category);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await Sort(query, sortBy, descending)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Book>> GetForExportAsync(
        string? search,
        string? category,
        string sortBy,
        bool descending,
        int maximumRows,
        CancellationToken cancellationToken) =>
        await Sort(Filter(search, category).AsNoTracking(), sortBy, descending)
            .Take(maximumRows)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetExistingIsbnsAsync(
        IEnumerable<string> isbns,
        CancellationToken cancellationToken)
    {
        var values = isbns.Distinct(StringComparer.Ordinal).ToArray();
        return (await dbContext.Books.AsNoTracking()
                .Where(book => values.Contains(book.Isbn))
                .Select(book => book.Isbn)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Borrowings.AnyAsync(item => item.BookId == id, cancellationToken) ||
        await dbContext.Reservations.AnyAsync(item => item.BookId == id, cancellationToken) ||
        await dbContext.BookCopies.AnyAsync(item => item.BookId == id, cancellationToken) ||
        await dbContext.StockReceiptItems.AnyAsync(item => item.BookId == id, cancellationToken);

    private IQueryable<Book> Filter(string? search, string? category)
    {
        var query = dbContext.Books.AsNoTracking()
            .Where(book => book.Status == RecordStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{keyword}%") ||
                EF.Functions.ILike(book.Author, $"%{keyword}%") ||
                EF.Functions.ILike(book.Isbn, $"%{keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(book => book.Category == category.Trim());

        return query;
    }

    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        IReadOnlyCollection<Guid>? authorIds,
        IReadOnlyCollection<Guid>? categoryIds,
        Guid? publisherId,
        RecordStatus? status,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Books.AsQueryable();
        if (status is not null)
            query = query.Where(book => book.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{keyword}%") ||
                EF.Functions.ILike(book.Author, $"%{keyword}%") ||
                EF.Functions.ILike(book.Isbn, $"%{keyword}%") ||
                dbContext.BookAuthors.Any(link => link.BookId == book.Id &&
                    dbContext.Authors.Any(author => author.Id == link.AuthorId &&
                        EF.Functions.ILike(author.FullName, $"%{keyword}%"))) ||
                dbContext.BookCategories.Any(link => link.BookId == book.Id &&
                    dbContext.Categories.Any(bookCategory => bookCategory.Id == link.CategoryId &&
                        EF.Functions.ILike(bookCategory.Name, $"%{keyword}%"))) ||
                (book.PublisherId != null && dbContext.Publishers.Any(publisher =>
                    publisher.Id == book.PublisherId && EF.Functions.ILike(publisher.Name, $"%{keyword}%"))));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(book => book.Category == category.Trim() ||
                dbContext.BookCategories.Any(link => link.BookId == book.Id &&
                    dbContext.Categories.Any(bookCategory => bookCategory.Id == link.CategoryId &&
                        bookCategory.Name == category.Trim())));
        if (authorIds is { Count: > 0 })
            query = query.Where(book => dbContext.BookAuthors.Any(link =>
                link.BookId == book.Id && authorIds.Contains(link.AuthorId)));
        if (categoryIds is { Count: > 0 })
            query = query.Where(book => dbContext.BookCategories.Any(link =>
                link.BookId == book.Id && categoryIds.Contains(link.CategoryId)));
        if (publisherId is not null)
            query = query.Where(book => book.PublisherId == publisherId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await SortCatalog(query, sortBy, descending)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }
    private static IOrderedQueryable<Book> Sort(IQueryable<Book> query, string sortBy, bool descending) =>
        (sortBy, descending) switch
        {
            ("author", false) => query.OrderBy(book => book.Author).ThenBy(book => book.Id),
            ("author", true) => query.OrderByDescending(book => book.Author).ThenByDescending(book => book.Id),
            ("isbn", false) => query.OrderBy(book => book.Isbn).ThenBy(book => book.Id),
            ("isbn", true) => query.OrderByDescending(book => book.Isbn).ThenByDescending(book => book.Id),
            ("category", false) => query.OrderBy(book => book.Category).ThenBy(book => book.Title).ThenBy(book => book.Id),
            ("category", true) => query.OrderByDescending(book => book.Category).ThenByDescending(book => book.Title).ThenByDescending(book => book.Id),
            ("quantity", false) => query.OrderBy(book => book.Quantity).ThenBy(book => book.Title).ThenBy(book => book.Id),
            ("quantity", true) => query.OrderByDescending(book => book.Quantity).ThenByDescending(book => book.Title).ThenByDescending(book => book.Id),
            ("createdAtUtc", false) => query.OrderBy(book => book.CreatedAtUtc).ThenBy(book => book.Id),
            ("createdAtUtc", true) => query.OrderByDescending(book => book.CreatedAtUtc).ThenByDescending(book => book.Id),
            (_, true) => query.OrderByDescending(book => book.Title).ThenByDescending(book => book.Id),
            _ => query.OrderBy(book => book.Title).ThenBy(book => book.Id)
        };

    private IOrderedQueryable<Book> SortCatalog(IQueryable<Book> query, string sortBy, bool descending) =>
        sortBy == "quantity"
            ? descending
                ? query.OrderByDescending(book => dbContext.BookCopies.Count(copy => copy.BookId == book.Id && copy.Status == CopyStatus.Available)).ThenByDescending(book => book.Id)
                : query.OrderBy(book => dbContext.BookCopies.Count(copy => copy.BookId == book.Id && copy.Status == CopyStatus.Available)).ThenBy(book => book.Id)
            : Sort(query, sortBy, descending);

    public void Remove(Book book) => dbContext.Books.Remove(book);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, BookCatalogSnapshot>> LoadCatalogAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        if (bookIds.Count == 0)
            return new Dictionary<Guid, BookCatalogSnapshot>();

        var authors = await dbContext.BookAuthors
            .Where(link => bookIds.Contains(link.BookId))
            .Join(
                dbContext.Authors,
                link => link.AuthorId,
                author => author.Id,
                (link, author) => new
                {
                    link.BookId,
                    Reference = new BookCatalogReference(author.Id, author.FullName)
                })
            .ToListAsync(cancellationToken);
        var categories = await dbContext.BookCategories
            .Where(link => bookIds.Contains(link.BookId))
            .Join(
                dbContext.Categories,
                link => link.CategoryId,
                category => category.Id,
                (link, category) => new
                {
                    link.BookId,
                    Reference = new BookCatalogReference(category.Id, category.Name)
                })
            .ToListAsync(cancellationToken);
        var publishers = await dbContext.Books
            .Where(book => bookIds.Contains(book.Id) && book.PublisherId != null)
            .Join(
                dbContext.Publishers,
                book => book.PublisherId,
                publisher => publisher.Id,
                (book, publisher) => new
                {
                    BookId = book.Id,
                    Reference = new BookCatalogReference(publisher.Id, publisher.Name)
                })
            .ToListAsync(cancellationToken);
        var available = await dbContext.BookCopies
            .Where(copy => bookIds.Contains(copy.BookId) && copy.Status == CopyStatus.Available)
            .GroupBy(copy => copy.BookId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return bookIds.Distinct().ToDictionary(
            bookId => bookId,
            bookId => new BookCatalogSnapshot(
                authors.Where(item => item.BookId == bookId).Select(item => item.Reference).ToArray(),
                categories.Where(item => item.BookId == bookId).Select(item => item.Reference).ToArray(),
                publishers.Where(item => item.BookId == bookId).Select(item => item.Reference).FirstOrDefault(),
                available.GetValueOrDefault(bookId)));
    }
}
