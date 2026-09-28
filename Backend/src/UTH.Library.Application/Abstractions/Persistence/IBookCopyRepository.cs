using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record BookCopyQuery(string? Search, Guid? BookId, Guid? BranchId, Guid? ShelfId,
    CopyCondition? Condition, CopyStatus? Status, int PageNumber, int PageSize);

public sealed record BookCopySnapshot(BookCopy Copy, string BookTitle, string? ShelfCode,
    Guid? BranchId, string? BranchCode);

public interface IBookCopyRepository
{
    Task<PageResult<BookCopySnapshot>> GetPageAsync(BookCopyQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookCopySnapshot>> GetExportAsync(BookCopyQuery query, CancellationToken cancellationToken);
    Task<BookCopySnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BookCopySnapshot?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<BookCopy?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken);
    Task<bool> ActiveBookExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ActiveShelfExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetActiveBookIdsByIsbnAsync(string isbn, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetActiveShelfIdsByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> ReceiptItemMatchesBookAsync(Guid id, Guid bookId, CancellationToken cancellationToken);
    Task<bool> HasActiveAuditAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasEditableReceiptAsync(Guid? stockReceiptItemId, CancellationToken cancellationToken);
    Task<bool> HasActiveBorrowingForBookAsync(Guid bookId, CancellationToken cancellationToken);
    Task AddAsync(BookCopy copy, CancellationToken cancellationToken);
    void DiscardPendingChanges();
}
