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
        var authorsValid = authorIds.Count == 0 || await dbContext.Authors
            .CountAsync(author => authorIds.Contains(author.Id) && author.Status == RecordStatus.Active, cancellationToken) == authorIds.Count;
        var categoriesValid = categoryIds.Count == 0 || await dbContext.Categories
            .CountAsync(category => categoryIds.Contains(category.Id) && category.Status == RecordStatus.Active, cancellationToken) == categoryIds.Count;
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
        var existingAuthors = await dbContext.BookAuthors.Where(link => link.BookId == bookId).ToListAsync(cancellationToken);
        var existingCategories = await dbContext.BookCategories.Where(link => link.BookId == bookId).ToListAsync(cancellationToken);
        var existingPublishers = await dbContext.BookPublishers.Where(link => link.BookId == bookId).ToListAsync(cancellationToken);
        dbContext.BookAuthors.RemoveRange(existingAuthors);
        dbContext.BookCategories.RemoveRange(existingCategories);
        dbContext.BookPublishers.RemoveRange(existingPublishers);

        await dbContext.BookAuthors.AddRangeAsync(
            authorIds.Distinct().Select(authorId => BookAuthor.Create(bookId, authorId)), cancellationToken);
        await dbContext.BookCategories.AddRangeAsync(
            categoryIds.Distinct().Select(categoryId => BookCategory.Create(bookId, categoryId)), cancellationToken);
        if (publisherId is not null)
            await dbContext.BookPublishers.AddAsync(BookPublisher.Create(bookId, publisherId.Value), cancellationToken);
    }

    public async Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Books.AsQueryable();

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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(book => book.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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
        string sortDirection,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Books
            .Where(book => status == null || book.Status == status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{keyword}%") ||
                EF.Functions.ILike(book.Author, $"%{keyword}%") ||
                EF.Functions.ILike(book.Isbn, $"%{keyword}%") ||
                dbContext.BookAuthors.Any(link => link.BookId == book.Id &&
                    dbContext.Authors.Any(author => author.Id == link.AuthorId && EF.Functions.ILike(author.FullName, $"%{keyword}%"))) ||
                dbContext.BookCategories.Any(link => link.BookId == book.Id &&
                    dbContext.Categories.Any(bookCategory => bookCategory.Id == link.CategoryId && EF.Functions.ILike(bookCategory.Name, $"%{keyword}%"))));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(book => book.Category == category.Trim() ||
                dbContext.BookCategories.Any(link => link.BookId == book.Id &&
                    dbContext.Categories.Any(bookCategory => bookCategory.Id == link.CategoryId && bookCategory.Name == category.Trim())));
        if (authorIds is { Count: > 0 })
            query = query.Where(book => dbContext.BookAuthors.Any(link => link.BookId == book.Id && authorIds.Contains(link.AuthorId)));
        if (categoryIds is { Count: > 0 })
            query = query.Where(book => dbContext.BookCategories.Any(link => link.BookId == book.Id && categoryIds.Contains(link.CategoryId)));
        if (publisherId is not null)
            query = query.Where(book => dbContext.BookPublishers.Any(link => link.BookId == book.Id && link.PublisherId == publisherId));

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy.Trim().ToLowerInvariant() switch
        {
            "isbn" => descending ? query.OrderByDescending(book => book.Isbn) : query.OrderBy(book => book.Isbn),
            "createdatutc" => descending ? query.OrderByDescending(book => book.CreatedAtUtc) : query.OrderBy(book => book.CreatedAtUtc),
            "status" => descending ? query.OrderByDescending(book => book.Status) : query.OrderBy(book => book.Status),
            _ => descending ? query.OrderByDescending(book => book.Title) : query.OrderBy(book => book.Title)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

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
            .Join(dbContext.Authors, link => link.AuthorId, author => author.Id,
                (link, author) => new { link.BookId, Reference = new BookCatalogReference(author.Id, author.FullName) })
            .ToListAsync(cancellationToken);
        var categories = await dbContext.BookCategories
            .Where(link => bookIds.Contains(link.BookId))
            .Join(dbContext.Categories, link => link.CategoryId, category => category.Id,
                (link, category) => new { link.BookId, Reference = new BookCatalogReference(category.Id, category.Name) })
            .ToListAsync(cancellationToken);
        var publishers = await dbContext.BookPublishers
            .Where(link => bookIds.Contains(link.BookId))
            .Join(dbContext.Publishers, link => link.PublisherId, publisher => publisher.Id,
                (link, publisher) => new { link.BookId, Reference = new BookCatalogReference(publisher.Id, publisher.Name) })
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
