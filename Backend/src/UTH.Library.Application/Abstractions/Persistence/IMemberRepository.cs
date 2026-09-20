using UTH.Library.Domain.Entities;
namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record MemberQuery(string? Search, MemberStatus? Status, string? MemberGroup, int PageNumber, int PageSize);
public sealed record MemberHistoryData(IReadOnlyList<Borrowing> Borrowings, IReadOnlyList<Reservation> Reservations,
    IReadOnlyList<Violation> Violations, IReadOnlyList<FinePayment> Payments, IReadOnlyList<FineAdjustment> Adjustments);
public sealed record MemberHistoryRecord(Guid Id, string Type, DateTime OccurredAtUtc, string Title,
    string Description, decimal? Amount);
public interface IMemberRepository
{
    Task<(IReadOnlyList<Member>, int)> GetAsync(MemberQuery query, CancellationToken ct);
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct);
    Task<Member?> GetByCardOrCodeAsync(string cardOrCode, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken ct);
    Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken ct);
    Task<MemberHistoryData> GetHistoryAsync(Guid memberId, CancellationToken ct);
    Task<(IReadOnlyList<MemberHistoryRecord> Items, int TotalCount)> GetHistoryPageAsync(
        Guid memberId, string category, int pageNumber, int pageSize, CancellationToken ct);
    Task<Violation?> GetViolationAsync(Guid memberId, Guid violationId, CancellationToken ct);
    Task<decimal> GetViolationBalanceAsync(Guid memberId, Guid violationId, decimal originalAmount, CancellationToken ct);
    Task AddMemberAsync(Member member, CancellationToken ct);
    Task AddCardAsync(MembershipCard card, CancellationToken ct);
    Task AddRestrictionAsync(MemberRestriction restriction, CancellationToken ct);
    Task<MemberRestriction?> GetRestrictionAsync(Guid memberId, Guid restrictionId, CancellationToken ct);
    Task AddPaymentAsync(FinePayment payment, CancellationToken ct);
    Task AddAdjustmentAsync(FineAdjustment adjustment, CancellationToken ct);
    Task AddAuditLogAsync(AuditLog log, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
