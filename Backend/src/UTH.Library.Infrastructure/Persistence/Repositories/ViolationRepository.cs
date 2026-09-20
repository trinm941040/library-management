using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class ViolationRepository(LibraryDbContext dbContext) : IViolationRepository
{
    public Task AddAsync(Violation violation, CancellationToken cancellationToken) =>
        dbContext.Violations.AddAsync(violation, cancellationToken).AsTask();

    public Task<Violation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Violations.SingleOrDefaultAsync(violation => violation.Id == id, cancellationToken);

    public async Task<Violation?> GetExistingViolationAsync(Guid? borrowingId, Guid? bookCopyId, string type, CancellationToken cancellationToken)
    {
        if (borrowingId is null && bookCopyId is null) return null;
        var normalizedType = type.Trim().ToLowerInvariant();

        var candidates = await dbContext.Violations
            .Where(v => v.Type == normalizedType && v.ResolvedAtUtc == null)
            .OrderByDescending(v => v.RecordedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (borrowingId.HasValue && candidate.ExtractBorrowingId() == borrowingId.Value)
                return candidate;
            if (bookCopyId.HasValue && candidate.ExtractBookCopyId() == bookCopyId.Value)
                return candidate;
        }

        return null;
    }

    public async Task<(IReadOnlyList<Violation> Items, int TotalCount)> GetPageAsync(
        string? search,
        Guid? borrowerId,
        string? type,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        bool? hasBalanceOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Violations.AsQueryable();

        if (borrowerId.HasValue && borrowerId.Value != Guid.Empty)
            query = query.Where(v => v.BorrowerId == borrowerId.Value);

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
            query = query.Where(v => v.Type == type.Trim().ToLowerInvariant());

        if (fromDate.HasValue)
            query = query.Where(v => v.RecordedAtUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(v => v.RecordedAtUtc <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(violation =>
                EF.Functions.ILike(violation.BorrowerName, $"%{keyword}%") ||
                EF.Functions.ILike(violation.BorrowerEmail, $"%{keyword}%") ||
                EF.Functions.ILike(violation.BookTitle, $"%{keyword}%") ||
                EF.Functions.ILike(violation.Note, $"%{keyword}%"));
        }

        if (hasBalanceOnly == true)
        {
            query = query.Where(v => v.ResolvedAtUtc == null);
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "open" => query.Where(violation => violation.ResolvedAtUtc == null),
            "paid" => query.Where(violation => violation.Resolution == "paid"),
            "waived" => query.Where(violation => violation.Resolution == "waived"),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(violation => violation.RecordedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(decimal TotalAdjusted, decimal TotalPaid, decimal Balance)> GetFinanceSummaryAsync(
        Guid violationId,
        decimal originalAmount,
        CancellationToken cancellationToken)
    {
        var totalAdjusted = await dbContext.FineAdjustments
            .Where(x => x.ViolationId == violationId)
            .SumAsync(x => (decimal?)x.AmountDelta, cancellationToken) ?? 0m;

        var totalPaid = await dbContext.FinePayments
            .Where(x => x.ViolationId == violationId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var balance = Math.Max(0m, originalAmount + totalAdjusted - totalPaid);
        return (totalAdjusted, totalPaid, balance);
    }

    public async Task<(IReadOnlyList<FinePayment> Payments, IReadOnlyList<FineAdjustment> Adjustments)> GetFinanceTransactionsAsync(
        Guid violationId,
        CancellationToken cancellationToken)
    {
        var payments = await dbContext.FinePayments
            .Where(x => x.ViolationId == violationId)
            .OrderByDescending(x => x.PaidAtUtc)
            .ToListAsync(cancellationToken);

        var adjustments = await dbContext.FineAdjustments
            .Where(x => x.ViolationId == violationId)
            .OrderByDescending(x => x.AdjustedAtUtc)
            .ToListAsync(cancellationToken);

        return (payments, adjustments);
    }

    public Task AddPaymentAsync(FinePayment payment, CancellationToken cancellationToken) =>
        dbContext.FinePayments.AddAsync(payment, cancellationToken).AsTask();

    public Task AddAdjustmentAsync(FineAdjustment adjustment, CancellationToken cancellationToken) =>
        dbContext.FineAdjustments.AddAsync(adjustment, cancellationToken).AsTask();

    public async Task<IReadOnlyList<FineAdjustment>> GetAdjustmentsByViolationIdAsync(Guid violationId, CancellationToken cancellationToken) =>
        await dbContext.FineAdjustments
            .Where(item => item.ViolationId == violationId)
            .OrderByDescending(item => item.AdjustedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<decimal> GetTotalAdjustedAsync(Guid violationId, CancellationToken cancellationToken) =>
        dbContext.FineAdjustments
            .Where(item => item.ViolationId == violationId)
            .SumAsync(item => (decimal?)item.AmountDelta, cancellationToken)
            .ContinueWith(task => task.Result ?? 0m, cancellationToken);

    public Task<decimal> GetTotalPaidAsync(Guid violationId, CancellationToken cancellationToken) =>
        dbContext.FinePayments
            .Where(item => item.ViolationId == violationId)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken)
            .ContinueWith(task => task.Result ?? 0m, cancellationToken);

    public Task<FinePayment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        dbContext.FinePayments.SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

    public async Task<FinePayment?> GetExistingPaymentAsync(Guid violationId, string? reference, decimal amount, DateTime windowStart, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(reference))
        {
            var normalizedRef = reference.Trim();
            var byRef = await dbContext.FinePayments
                .Where(x => x.ViolationId == violationId && x.Reference == normalizedRef)
                .OrderByDescending(x => x.PaidAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byRef is not null) return byRef;
        }

        return await dbContext.FinePayments
            .Where(x => x.ViolationId == violationId && x.Amount == amount && x.PaidAtUtc >= windowStart)
            .OrderByDescending(x => x.PaidAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<FinePayment> Items, int TotalCount)> GetPaymentsPageAsync(
        string? search,
        Guid? violationId,
        Guid? memberId,
        FinePaymentMethod? method,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FinePayments.AsQueryable();

        if (violationId.HasValue && violationId.Value != Guid.Empty)
            query = query.Where(x => x.ViolationId == violationId.Value);

        if (memberId.HasValue && memberId.Value != Guid.Empty)
            query = query.Where(x => x.MemberId == memberId.Value);

        if (method.HasValue)
            query = query.Where(x => x.Method == method.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.PaidAtUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.PaidAtUtc <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Reference, $"%{keyword}%") ||
                dbContext.Violations.Any(v => v.Id == x.ViolationId && (
                    EF.Functions.ILike(v.BorrowerName, $"%{keyword}%") ||
                    EF.Functions.ILike(v.BorrowerEmail, $"%{keyword}%") ||
                    EF.Functions.ILike(v.BookTitle, $"%{keyword}%"))) ||
                dbContext.Members.Any(m => m.Id == x.MemberId && (
                    EF.Functions.ILike(m.FullName, $"%{keyword}%") ||
                    EF.Functions.ILike(m.MemberCode, $"%{keyword}%") ||
                    EF.Functions.ILike(m.Email, $"%{keyword}%"))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.PaidAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
