using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

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
                !dbContext.BookCopies.Any(copy => copy.BookId == reservation.BookId && copy.Status == CopyStatus.Available)),
            "ready" => query.Where(reservation =>
                reservation.FulfilledAtUtc == null &&
                reservation.CancelledAtUtc == null &&
                reservation.ExpiresAtUtc >= utcNow &&
                dbContext.BookCopies.Any(copy => copy.BookId == reservation.BookId && copy.Status == CopyStatus.Available)),
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

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
