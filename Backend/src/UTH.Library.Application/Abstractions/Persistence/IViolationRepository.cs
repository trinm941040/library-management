using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IViolationRepository
{
    Task AddAsync(Violation violation, CancellationToken cancellationToken);
    Task<Violation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Violation?> GetExistingViolationAsync(Guid? borrowingId, Guid? bookCopyId, string type, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Violation> Items, int TotalCount)> GetPageAsync(
        string? search,
        Guid? borrowerId,
        string? type,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        bool? hasBalanceOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<(decimal TotalAdjusted, decimal TotalPaid, decimal Balance)> GetFinanceSummaryAsync(Guid violationId, decimal originalAmount, CancellationToken cancellationToken);
    Task<(IReadOnlyList<FinePayment> Payments, IReadOnlyList<FineAdjustment> Adjustments)> GetFinanceTransactionsAsync(Guid violationId, CancellationToken cancellationToken);
    Task AddPaymentAsync(FinePayment payment, CancellationToken cancellationToken);
    Task<FinePayment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<FinePayment?> GetExistingPaymentAsync(Guid violationId, string? reference, decimal amount, DateTime windowStart, CancellationToken cancellationToken);
    Task<(IReadOnlyList<FinePayment> Items, int TotalCount)> GetPaymentsPageAsync(
        string? search,
        Guid? violationId,
        Guid? memberId,
        FinePaymentMethod? method,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task AddAdjustmentAsync(FineAdjustment adjustment, CancellationToken cancellationToken);
    Task<IReadOnlyList<FineAdjustment>> GetAdjustmentsByViolationIdAsync(Guid violationId, CancellationToken cancellationToken);
    Task<decimal> GetTotalAdjustedAsync(Guid violationId, CancellationToken cancellationToken);
    Task<decimal> GetTotalPaidAsync(Guid violationId, CancellationToken cancellationToken);
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
