using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class MemberRepository(LibraryDbContext db) : IMemberRepository
{
    public async Task<(IReadOnlyList<Member>, int)> GetAsync(MemberQuery q, CancellationToken ct) {
        var query=db.Members.AsNoTracking().Include(x=>x.MembershipCard).AsQueryable();
        if(!string.IsNullOrWhiteSpace(q.Search)){var s=q.Search.Trim();query=query.Where(x=>EF.Functions.ILike(x.MemberCode,$"%{s}%")||EF.Functions.ILike(x.FullName,$"%{s}%")||EF.Functions.ILike(x.Email,$"%{s}%")||(x.PhoneNumber!=null&&EF.Functions.ILike(x.PhoneNumber,$"%{s}%"))||(x.MembershipCard!=null&&EF.Functions.ILike(x.MembershipCard.CardNumber,$"%{s}%")));}
        if(q.Status is not null)query=query.Where(x=>x.Status==q.Status); if(!string.IsNullOrWhiteSpace(q.MemberGroup))query=query.Where(x=>x.MemberGroup==q.MemberGroup.Trim());
        var count=await query.CountAsync(ct);var items=await query.OrderBy(x=>x.MemberCode).ThenBy(x=>x.Id).Skip((q.PageNumber-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(ct);return(items,count);
    }
    public Task<Member?> GetByIdAsync(Guid id,CancellationToken ct)=>db.Members.Include(x=>x.MembershipCard).Include(x=>x.Restrictions).SingleOrDefaultAsync(x=>x.Id==id,ct);
    public Task<bool> CodeExistsAsync(string code,Guid? id,CancellationToken ct)=>db.Members.AnyAsync(x=>x.MemberCode==code&&(id==null||x.Id!=id),ct);
    public Task<bool> EmailExistsAsync(string email,Guid? id,CancellationToken ct)=>db.Members.AnyAsync(x=>x.Email==email&&(id==null||x.Id!=id),ct);
    public Task<bool> CardNumberExistsAsync(string n,CancellationToken ct)=>db.MembershipCards.AnyAsync(x=>x.CardNumber==n,ct);
    public async Task<MemberHistoryData> GetHistoryAsync(Guid id,CancellationToken ct)=>new(
        await db.Borrowings.AsNoTracking().Where(x=>x.BorrowerId==id).OrderByDescending(x=>x.BorrowedAtUtc).ToArrayAsync(ct),
        await db.Reservations.AsNoTracking().Where(x=>x.ReserverId==id).OrderByDescending(x=>x.ReservedAtUtc).ToArrayAsync(ct),
        await db.Violations.AsNoTracking().Where(x=>x.BorrowerId==id).OrderByDescending(x=>x.RecordedAtUtc).ToArrayAsync(ct),
        await db.FinePayments.AsNoTracking().Where(x=>x.MemberId==id).ToArrayAsync(ct),await db.FineAdjustments.AsNoTracking().Where(x=>x.MemberId==id).ToArrayAsync(ct));
    public async Task<(IReadOnlyList<MemberHistoryRecord> Items, int TotalCount)> GetHistoryPageAsync(
        Guid memberId, string category, int pageNumber, int pageSize, CancellationToken ct)
    {
        IQueryable<MemberHistoryRecord> query = category switch
        {
            "Borrowings" =>
                from borrowing in db.Borrowings.AsNoTracking()
                join book in db.Books.AsNoTracking() on borrowing.BookId equals book.Id
                where borrowing.BorrowerId == memberId
                orderby borrowing.BorrowedAtUtc descending, borrowing.Id descending
                select new MemberHistoryRecord(
                    borrowing.Id, "borrowing", borrowing.BorrowedAtUtc, book.Title,
                    borrowing.ReturnedAtUtc == null ? "Đang mượn" : "Đã trả", null),
            "Returns" =>
                from borrowing in db.Borrowings.AsNoTracking()
                join book in db.Books.AsNoTracking() on borrowing.BookId equals book.Id
                where borrowing.BorrowerId == memberId && borrowing.ReturnedAtUtc != null
                orderby borrowing.ReturnedAtUtc descending, borrowing.Id descending
                select new MemberHistoryRecord(
                    borrowing.Id, "return", borrowing.ReturnedAtUtc!.Value, book.Title,
                    "Đã trả sách", null),
            "Renewals" =>
                from renewal in db.Renewals.AsNoTracking()
                join borrowing in db.Borrowings.AsNoTracking() on renewal.BorrowingId equals borrowing.Id
                join book in db.Books.AsNoTracking() on borrowing.BookId equals book.Id
                where borrowing.BorrowerId == memberId
                orderby renewal.RenewedAtUtc descending, renewal.Id descending
                select new MemberHistoryRecord(
                    renewal.Id, "renewal", renewal.RenewedAtUtc, book.Title,
                    "Đã gia hạn thời gian mượn", null),
            "Reservations" =>
                from reservation in db.Reservations.AsNoTracking()
                join book in db.Books.AsNoTracking() on reservation.BookId equals book.Id
                where reservation.ReserverId == memberId
                orderby reservation.ReservedAtUtc descending, reservation.Id descending
                select new MemberHistoryRecord(
                    reservation.Id, "reservation", reservation.ReservedAtUtc, book.Title,
                    reservation.FulfilledAtUtc != null ? "Đã nhận" :
                    reservation.CancelledAtUtc != null ? "Đã hủy" : "Đang giữ chỗ", null),
            "Violations" => db.Violations.AsNoTracking()
                .Where(violation => violation.BorrowerId == memberId)
                .OrderByDescending(violation => violation.RecordedAtUtc)
                .ThenByDescending(violation => violation.Id)
                .Select(violation => new MemberHistoryRecord(
                    violation.Id, "violation", violation.RecordedAtUtc,
                    violation.BookTitle, violation.Note, violation.FineAmount)),
            "Payments" =>
                from payment in db.FinePayments.AsNoTracking()
                join violation in db.Violations.AsNoTracking() on payment.ViolationId equals violation.Id
                where payment.MemberId == memberId
                orderby payment.PaidAtUtc descending, payment.Id descending
                select new MemberHistoryRecord(
                    payment.Id, "payment", payment.PaidAtUtc, violation.BookTitle,
                    payment.Reference ?? "Thanh toán tiền phạt", payment.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(category), "History category is invalid.")
        };

        var count = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);
        return (items, count);
    }
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
