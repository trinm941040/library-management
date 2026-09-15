using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class MemberRepository(LibraryDbContext db) : IMemberRepository
{
    public async Task<(IReadOnlyList<Member>, int)> GetAsync(MemberQuery q, CancellationToken ct) {
        var query=db.Members.AsNoTracking().Include(x=>x.MembershipCard).AsQueryable();
        if(!string.IsNullOrWhiteSpace(q.Search)){var s=q.Search.Trim();query=query.Where(x=>EF.Functions.ILike(x.MemberCode,$"%{s}%")||EF.Functions.ILike(x.FullName,$"%{s}%")||EF.Functions.ILike(x.Email,$"%{s}%")||(x.PhoneNumber!=null&&EF.Functions.ILike(x.PhoneNumber,$"%{s}%")));}
        if(q.Status is not null)query=query.Where(x=>x.Status==q.Status); if(!string.IsNullOrWhiteSpace(q.MemberGroup))query=query.Where(x=>x.MemberGroup==q.MemberGroup.Trim());
        var count=await query.CountAsync(ct);var items=await query.OrderBy(x=>x.MemberCode).Skip((q.PageNumber-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(ct);return(items,count);
    }
    public Task<Member?> GetByIdAsync(Guid id,CancellationToken ct)=>db.Members.Include(x=>x.MembershipCard).Include(x=>x.Restrictions).SingleOrDefaultAsync(x=>x.Id==id,ct);
    public Task<Member?> GetByCardOrCodeAsync(string cardOrCode, CancellationToken ct)
    {
        var normalized = cardOrCode.Trim().ToUpperInvariant();
        return db.Members
            .Include(x => x.MembershipCard)
            .Include(x => x.Restrictions)
            .SingleOrDefaultAsync(
                x => x.MemberCode == normalized ||
                     (x.MembershipCard != null && x.MembershipCard.CardNumber == normalized),
                ct);
    }
    public Task<bool> CodeExistsAsync(string code,Guid? id,CancellationToken ct)=>db.Members.AnyAsync(x=>x.MemberCode==code&&(id==null||x.Id!=id),ct);
    public Task<bool> EmailExistsAsync(string email,Guid? id,CancellationToken ct)=>db.Members.AnyAsync(x=>x.Email==email&&(id==null||x.Id!=id),ct);
    public Task<bool> CardNumberExistsAsync(string n,CancellationToken ct)=>db.MembershipCards.AnyAsync(x=>x.CardNumber==n,ct);
    public async Task<MemberHistoryData> GetHistoryAsync(Guid id,CancellationToken ct)=>new(
        await db.Borrowings.AsNoTracking().Where(x=>x.BorrowerId==id).OrderByDescending(x=>x.BorrowedAtUtc).ToArrayAsync(ct),
        await db.Reservations.AsNoTracking().Where(x=>x.ReserverId==id).OrderByDescending(x=>x.ReservedAtUtc).ToArrayAsync(ct),
        await db.Violations.AsNoTracking().Where(x=>x.BorrowerId==id).OrderByDescending(x=>x.RecordedAtUtc).ToArrayAsync(ct),
        await db.FinePayments.AsNoTracking().Where(x=>x.MemberId==id).ToArrayAsync(ct),await db.FineAdjustments.AsNoTracking().Where(x=>x.MemberId==id).ToArrayAsync(ct));
    public Task<Violation?> GetViolationAsync(Guid id,Guid violationId,CancellationToken ct)=>db.Violations.SingleOrDefaultAsync(x=>x.Id==violationId&&x.BorrowerId==id,ct);
    public async Task<decimal> GetViolationBalanceAsync(Guid id, Guid violationId, decimal original, CancellationToken ct)
    {
        var adjustments = await db.FineAdjustments
            .Where(x => x.MemberId == id && x.ViolationId == violationId)
            .SumAsync(x => (decimal?)x.AmountDelta, ct) ?? 0;
        var payments = await db.FinePayments
            .Where(x => x.MemberId == id && x.ViolationId == violationId)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        return Math.Max(0, original + adjustments - payments);
    }
    public Task AddMemberAsync(Member x,CancellationToken ct)=>db.Members.AddAsync(x,ct).AsTask(); public Task AddCardAsync(MembershipCard x,CancellationToken ct)=>db.MembershipCards.AddAsync(x,ct).AsTask(); public Task AddRestrictionAsync(MemberRestriction x,CancellationToken ct)=>db.MemberRestrictions.AddAsync(x,ct).AsTask();
    public Task<MemberRestriction?> GetRestrictionAsync(Guid id,Guid rid,CancellationToken ct)=>db.MemberRestrictions.SingleOrDefaultAsync(x=>x.MemberId==id&&x.Id==rid,ct);
    public Task AddPaymentAsync(FinePayment x,CancellationToken ct)=>db.FinePayments.AddAsync(x,ct).AsTask(); public Task AddAdjustmentAsync(FineAdjustment x,CancellationToken ct)=>db.FineAdjustments.AddAsync(x,ct).AsTask(); public Task AddAuditLogAsync(AuditLog x,CancellationToken ct)=>db.AuditLogs.AddAsync(x,ct).AsTask(); public Task SaveChangesAsync(CancellationToken ct)=>db.SaveChangesAsync(ct);
}
