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
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Book>> GetForExportAsync(
        string? search,
        string? category,
        string sortBy,
        bool descending,
        int maximumRows,
        CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetExistingIsbnsAsync(
        IEnumerable<string> isbns,
        CancellationToken cancellationToken);
    Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken);
    void Remove(Book book);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
