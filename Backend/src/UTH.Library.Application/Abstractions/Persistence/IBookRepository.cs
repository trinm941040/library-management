using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task AddAsync(Book book, CancellationToken cancellationToken);
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    void Remove(Book book);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
