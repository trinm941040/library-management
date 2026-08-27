using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken);
    Task<Borrowing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Borrowing> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken);
    Task<bool> HasActiveBorrowingAsync(Guid bookId, Guid borrowerId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
