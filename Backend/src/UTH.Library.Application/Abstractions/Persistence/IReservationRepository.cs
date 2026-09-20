using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken);
    Task<bool> HasOpenReservationAsync(Guid bookId, Guid reserverId, CancellationToken cancellationToken);
    Task<Reservation?> GetFirstWaitingReservationForBookAsync(Guid bookId, DateTime utcNow, CancellationToken cancellationToken);
    Task<int> GetQueuePositionAsync(Guid bookId, Guid reservationId, DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reservation>> GetActiveReservationsForBookAsync(Guid bookId, DateTime utcNow, CancellationToken cancellationToken);
    Task<BookCopy?> GetAvailableBookCopyByBarcodeAsync(Guid bookId, string barcode, CancellationToken cancellationToken);
    Task<BookCopy?> GetFirstAvailableBookCopyAsync(Guid bookId, CancellationToken cancellationToken);
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
