using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken);
    Task AddRenewalAsync(Renewal renewal, CancellationToken cancellationToken);
    Task<Borrowing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Borrowing> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken);
    Task<bool> HasActiveBorrowingAsync(Guid bookId, Guid borrowerId, CancellationToken cancellationToken);
    Task<bool> HasActiveBorrowingForCopyAsync(Guid bookCopyId, CancellationToken cancellationToken);
    Task<Borrowing?> GetActiveBorrowingByCopyIdAsync(Guid bookCopyId, CancellationToken cancellationToken);
    Task<BookCopy?> GetBookCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<BookCopy?> GetBookCopyByIdAsync(Guid copyId, CancellationToken cancellationToken);
    Task<BookCopy?> GetFirstAvailableBookCopyAsync(Guid bookId, CancellationToken cancellationToken);
    Task<Guid?> GetEmployeeIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
