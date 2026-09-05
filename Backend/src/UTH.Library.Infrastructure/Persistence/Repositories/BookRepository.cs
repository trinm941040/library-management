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

    public void Remove(Book book) => dbContext.Books.Remove(book);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
