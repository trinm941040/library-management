using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(LibraryDbContext dbContext) : IReservationRepository
{
    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken) =>
        dbContext.Reservations.AddAsync(reservation, cancellationToken).AsTask();

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Reservations.SingleOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(reservation =>
                EF.Functions.ILike(reservation.ReserverName, $"%{keyword}%") ||
                EF.Functions.ILike(reservation.ReserverEmail, $"%{keyword}%"));
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "waiting" => query.Where(reservation =>
                reservation.FulfilledAtUtc == null &&
                reservation.CancelledAtUtc == null &&
                reservation.ExpiresAtUtc >= utcNow &&
                !dbContext.BookCopies.Any(copy =>
                    copy.BookId == reservation.BookId &&
                    (copy.Status == Domain.Enums.CopyStatus.Available || copy.Status == Domain.Enums.CopyStatus.Reserved) &&
                    !dbContext.Borrowings.Any(borrowing => borrowing.BookCopyId == copy.Id && borrowing.ReturnedAtUtc == null))),
            "ready" => query.Where(reservation =>
                reservation.FulfilledAtUtc == null &&
                reservation.CancelledAtUtc == null &&
                reservation.ExpiresAtUtc >= utcNow &&
                dbContext.BookCopies.Any(copy =>
                    copy.BookId == reservation.BookId &&
                    (copy.Status == Domain.Enums.CopyStatus.Available || copy.Status == Domain.Enums.CopyStatus.Reserved) &&
                    !dbContext.Borrowings.Any(borrowing => borrowing.BookCopyId == copy.Id && borrowing.ReturnedAtUtc == null))),
            "expired" => query.Where(reservation =>
                reservation.FulfilledAtUtc == null &&
                reservation.CancelledAtUtc == null &&
                reservation.ExpiresAtUtc < utcNow),
            "fulfilled" => query.Where(reservation => reservation.FulfilledAtUtc != null),
            "cancelled" => query.Where(reservation => reservation.CancelledAtUtc != null),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.ReservedAtUtc)
            .ThenByDescending(reservation => reservation.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> HasOpenReservationAsync(Guid bookId, Guid reserverId, CancellationToken cancellationToken) =>
        dbContext.Reservations.AnyAsync(
            reservation =>
                reservation.BookId == bookId &&
                reservation.ReserverId == reserverId &&
                reservation.FulfilledAtUtc == null &&
                reservation.CancelledAtUtc == null,
            cancellationToken);

    public Task<Reservation?> GetFirstWaitingReservationForBookAsync(Guid bookId, DateTime utcNow, CancellationToken cancellationToken) =>
        dbContext.Reservations
            .Where(r => r.BookId == bookId && r.FulfilledAtUtc == null && r.CancelledAtUtc == null && r.ExpiresAtUtc >= utcNow)
            .OrderBy(r => r.ReservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetQueuePositionAsync(Guid bookId, Guid reservationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var targetReservation = await dbContext.Reservations.FindAsync([reservationId], cancellationToken);
        if (targetReservation is null || targetReservation.FulfilledAtUtc != null || targetReservation.CancelledAtUtc != null)
            return 0;

        var countBefore = await dbContext.Reservations.CountAsync(
            r => r.BookId == bookId &&
                 r.FulfilledAtUtc == null &&
                 r.CancelledAtUtc == null &&
                 r.ExpiresAtUtc >= utcNow &&
                 r.ReservedAtUtc < targetReservation.ReservedAtUtc,
            cancellationToken);

        return countBefore + 1;
    }

    public async Task<IReadOnlyList<Reservation>> GetActiveReservationsForBookAsync(Guid bookId, DateTime utcNow, CancellationToken cancellationToken) =>
        await dbContext.Reservations
            .Where(r => r.BookId == bookId && r.FulfilledAtUtc == null && r.CancelledAtUtc == null && r.ExpiresAtUtc >= utcNow)
            .OrderBy(r => r.ReservedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<BookCopy?> GetAvailableBookCopyByBarcodeAsync(Guid bookId, string barcode, CancellationToken cancellationToken) =>
        dbContext.BookCopies.FirstOrDefaultAsync(
            c => c.BookId == bookId &&
                 c.Barcode == barcode &&
                 (c.Status == Domain.Enums.CopyStatus.Available || c.Status == Domain.Enums.CopyStatus.Reserved),
            cancellationToken);

    public Task<BookCopy?> GetFirstAvailableBookCopyAsync(Guid bookId, CancellationToken cancellationToken) =>
           dbContext.BookCopies
              .Where(
            c => c.BookId == bookId &&
                  (c.Status == Domain.Enums.CopyStatus.Available || c.Status == Domain.Enums.CopyStatus.Reserved) &&
                  !dbContext.Borrowings.Any(borrowing => borrowing.BookCopyId == c.Id && borrowing.ReturnedAtUtc == null))
              .OrderBy(c => c.Barcode)
              .FirstOrDefaultAsync(cancellationToken);

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
