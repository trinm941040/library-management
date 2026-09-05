using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class BorrowingRepository(LibraryDbContext dbContext) : IBorrowingRepository
{
    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken) =>
        dbContext.Borrowings.AddAsync(borrowing, cancellationToken).AsTask();

    public Task<Borrowing?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Borrowings.SingleOrDefaultAsync(borrowing => borrowing.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Borrowing> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Borrowings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(borrowing =>
                EF.Functions.ILike(borrowing.BorrowerName, $"%{keyword}%") ||
                EF.Functions.ILike(borrowing.BorrowerEmail, $"%{keyword}%"));
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "borrowed" => query.Where(borrowing => borrowing.ReturnedAtUtc == null && borrowing.DueAtUtc >= utcNow),
            "overdue" => query.Where(borrowing => borrowing.ReturnedAtUtc == null && borrowing.DueAtUtc < utcNow),
            "returned" => query.Where(borrowing => borrowing.ReturnedAtUtc != null),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(borrowing => borrowing.BorrowedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> HasActiveBorrowingAsync(Guid bookId, Guid borrowerId, CancellationToken cancellationToken) =>
        dbContext.Borrowings.AnyAsync(
            borrowing => borrowing.BookId == bookId && borrowing.BorrowerId == borrowerId && borrowing.ReturnedAtUtc == null,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
