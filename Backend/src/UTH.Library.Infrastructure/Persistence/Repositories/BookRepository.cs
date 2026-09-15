using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class BookRepository(LibraryDbContext dbContext) : IBookRepository
{
    public Task AddAsync(Book book, CancellationToken cancellationToken) =>
        dbContext.Books.AddAsync(book, cancellationToken).AsTask();

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Books.SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

    public Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId, CancellationToken cancellationToken) =>
        dbContext.Books.AnyAsync(
            book => book.Isbn == isbn && (excludeId == null || book.Id != excludeId),
            cancellationToken);

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
        var query = dbContext.Books.AsNoTracking().AsQueryable();

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

    public void Remove(Book book) => dbContext.Books.Remove(book);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
