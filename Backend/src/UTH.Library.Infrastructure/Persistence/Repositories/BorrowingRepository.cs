using Microsoft.EntityFrameworkCore;
using Npgsql;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class BorrowingRepository(LibraryDbContext dbContext) : IBorrowingRepository
{
    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken) =>
        dbContext.Borrowings.AddAsync(borrowing, cancellationToken).AsTask();

    public Task AddRenewalAsync(Renewal renewal, CancellationToken cancellationToken) =>
        dbContext.Renewals.AddAsync(renewal, cancellationToken).AsTask();

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
            .ThenByDescending(borrowing => borrowing.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> HasActiveBorrowingAsync(Guid bookId, Guid borrowerId, CancellationToken cancellationToken) =>
        dbContext.Borrowings.AnyAsync(
            borrowing => borrowing.BookId == bookId && borrowing.BorrowerId == borrowerId && borrowing.ReturnedAtUtc == null,
            cancellationToken);

    public Task<bool> HasActiveBorrowingForCopyAsync(Guid bookCopyId, CancellationToken cancellationToken) =>
        dbContext.Borrowings.AnyAsync(
            borrowing => borrowing.BookCopyId == bookCopyId && borrowing.ReturnedAtUtc == null,
            cancellationToken);

    public Task<Borrowing?> GetActiveBorrowingByCopyIdAsync(Guid bookCopyId, CancellationToken cancellationToken) =>
        dbContext.Borrowings
            .SingleOrDefaultAsync(
                borrowing => borrowing.BookCopyId == bookCopyId && borrowing.ReturnedAtUtc == null,
                cancellationToken);

    public Task<BookCopy?> GetBookCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var normalized = barcode.Trim().ToUpperInvariant();
        return dbContext.BookCopies.SingleOrDefaultAsync(
            copy => copy.Barcode == normalized,
            cancellationToken);
    }

    public Task<BookCopy?> GetBookCopyByIdAsync(Guid copyId, CancellationToken cancellationToken) =>
        dbContext.BookCopies.SingleOrDefaultAsync(
            copy => copy.Id == copyId,
            cancellationToken);

    public async Task<IReadOnlyList<Renewal>> GetRenewalsByBorrowingIdAsync(Guid borrowingId, CancellationToken cancellationToken) =>
        await dbContext.Renewals
            .Where(r => r.BorrowingId == borrowingId)
            .OrderByDescending(r => r.RenewedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
    public Task<decimal?> GetCopyUnitCostAsync(Guid? stockReceiptItemId, CancellationToken cancellationToken) =>
        dbContext.StockReceiptItems.Where(item => item.Id == stockReceiptItemId)
            .Select(item => item.UnitCost)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BookCopy?> GetFirstAvailableBookCopyAsync(Guid bookId, CancellationToken cancellationToken) =>
        dbContext.BookCopies.Where(copy => copy.BookId == bookId && copy.Status == CopyStatus.Available &&
            !dbContext.Borrowings.Any(borrowing => borrowing.BookCopyId == copy.Id && borrowing.ReturnedAtUtc == null))
            .OrderBy(copy => copy.Barcode)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Guid?> GetEmployeeIdByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Employees.Where(employee => employee.UserId == userId &&
            employee.Status == EmploymentStatus.Active)
            .Select(employee => (Guid?)employee.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_borrowings_ActiveBookCopyId" })
        {
            throw new OptimisticConcurrencyException("Book copy already has an active borrowing.", exception);
        }
    }
}
