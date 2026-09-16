using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
namespace UTH.Library.Application.Features.Members;

public sealed class MemberService(IMemberRepository repository, TimeProvider timeProvider)
{
    public async Task<MemberPage> GetAsync(MemberListQuery query, CancellationToken ct) {
        var pageNumber = Math.Max(1, query.PageNumber); var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, count) = await repository.GetAsync(new(query.Search, query.Status, query.MemberGroup, pageNumber, pageSize), ct);
        return new(items.Select(MapSummary).ToArray(), pageNumber, pageSize, count);
    }
    public async Task<MemberModel?> GetByIdAsync(Guid id, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member is null) return null;
        return await MapDetails(member, ct);
    }
    public async Task<MemberHistoryPage?> GetHistoryAsync(Guid id, MemberHistoryCategory category,
        int pageNumber, int pageSize, CancellationToken ct) {
        if (await repository.GetByIdAsync(id, ct) is null) return null;
        pageNumber = Math.Max(1, pageNumber); pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, count) = await repository.GetHistoryPageAsync(id, category.ToString(), pageNumber, pageSize, ct);
        return new MemberHistoryPage(items.Select(x => new MemberHistoryItemModel(
            x.Id, x.Type, x.OccurredAtUtc, x.Title, x.Description, x.Amount)).ToArray(),
            pageNumber, pageSize, count);
    }
    public async Task<MemberResult> CreateAsync(SaveMemberCommand command, Guid? actor, CancellationToken ct) {
        var duplicate = await Validate(command, null, ct); if (duplicate is not null) return duplicate;
        try { var now = timeProvider.GetUtcNow().UtcDateTime; var member = Member.Create(command.MemberCode, command.FullName, command.Email, command.PhoneNumber, command.DateOfBirth, command.Address, command.MemberGroup, command.Status, command.BorrowingLimit, command.LoanPeriodDays, now);
            await repository.AddMemberAsync(member, ct); await Audit("member.created", member.Id, null, member, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success(MapSummary(member)); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> UpdateAsync(Guid id, SaveMemberCommand command, Guid? actor, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member is null) return MemberResult.Fail(MemberFailure.NotFound, "Member was not found.");
        if (command.ConcurrencyToken is null) return MemberResult.Fail(MemberFailure.Validation, "Concurrency token is required.");
        if (command.ConcurrencyToken != member.ConcurrencyToken) return MemberResult.Fail(MemberFailure.Conflict, "Member was modified by another request.");
        var duplicate = await Validate(command, id, ct); if (duplicate is not null) return duplicate;
        try { var before = JsonSerializer.Serialize(member); var now = timeProvider.GetUtcNow().UtcDateTime; member.Update(command.MemberCode, command.FullName, command.Email, command.PhoneNumber, command.DateOfBirth, command.Address, command.MemberGroup, command.Status, command.BorrowingLimit, command.LoanPeriodDays, now);
            await Audit("member.updated", id, before, member, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success(await MapDetails(member, ct)); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> IssueCardAsync(Guid id, string number, DateOnly issuedOn, DateOnly expiresOn, Guid concurrencyToken, Guid? actor, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member is null) return MemberResult.Fail(MemberFailure.NotFound, "Member was not found.");
        if (member.ConcurrencyToken != concurrencyToken) return ConcurrencyConflict();
        if (member.MembershipCard is not null) return MemberResult.Fail(MemberFailure.Conflict, "Member already has a membership card.");
        if (await repository.CardNumberExistsAsync(number.Trim().ToUpperInvariant(), ct)) return MemberResult.Fail(MemberFailure.Conflict, "Card number already exists.");
        try { var now = timeProvider.GetUtcNow().UtcDateTime; var card = MembershipCard.Issue(id, number, issuedOn, expiresOn, now); await repository.AddCardAsync(card, ct); member.Touch(now); await Audit("membership-card.issued", nameof(MembershipCard), card.Id, null, card, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success((await GetByIdAsync(id, ct))!); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> RenewCardAsync(Guid id, DateOnly expiresOn, Guid concurrencyToken, Guid? actor, CancellationToken ct) => await ChangeCard(id, concurrencyToken, card => card.Renew(expiresOn, timeProvider.GetUtcNow().UtcDateTime), "membership-card.renewed", actor, ct);
    public async Task<MemberResult> ChangeCardStatusAsync(Guid id, MembershipCardStatus status, Guid concurrencyToken, Guid? actor, CancellationToken ct) => await ChangeCard(id, concurrencyToken, card => card.ChangeStatus(status, timeProvider.GetUtcNow().UtcDateTime), "membership-card.status-updated", actor, ct);
    private async Task<MemberResult> ChangeCard(Guid id, Guid concurrencyToken, Action<MembershipCard> change, string action, Guid? actor, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member?.MembershipCard is null) return MemberResult.Fail(MemberFailure.NotFound, "Membership card was not found.");
        if (member.ConcurrencyToken != concurrencyToken) return ConcurrencyConflict();
        try { var before = JsonSerializer.Serialize(member.MembershipCard); change(member.MembershipCard); var now = timeProvider.GetUtcNow().UtcDateTime; member.Touch(now); await Audit(action, nameof(MembershipCard), member.MembershipCard.Id, before, member.MembershipCard, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success(await MapDetails(member, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> AddRestrictionAsync(Guid id, MemberRestrictionType type, string reason, DateTime starts, DateTime? ends, Guid concurrencyToken, Guid? actor, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member is null) return MemberResult.Fail(MemberFailure.NotFound, "Member was not found.");
        if (member.ConcurrencyToken != concurrencyToken) return ConcurrencyConflict();
        try { var now = timeProvider.GetUtcNow().UtcDateTime; var restriction = MemberRestriction.Create(id, type, reason, starts.ToUniversalTime(), ends?.ToUniversalTime(), actor); await repository.AddRestrictionAsync(restriction, ct); member.Touch(now); await Audit("member-restriction.created", nameof(MemberRestriction), restriction.Id, null, restriction, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success((await GetByIdAsync(id, ct))!); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> RemoveRestrictionAsync(Guid id, Guid restrictionId, string reason, Guid concurrencyToken, Guid? actor, CancellationToken ct) {
        var member = await repository.GetByIdAsync(id, ct); if (member is null) return MemberResult.Fail(MemberFailure.NotFound, "Member was not found.");
        if (member.ConcurrencyToken != concurrencyToken) return ConcurrencyConflict();
        var restriction = await repository.GetRestrictionAsync(id, restrictionId, ct); if (restriction is null) return MemberResult.Fail(MemberFailure.NotFound, "Restriction was not found.");
        try { var before = JsonSerializer.Serialize(restriction); var now = timeProvider.GetUtcNow().UtcDateTime; restriction.Remove(reason, now, actor); member.Touch(now); await Audit("member-restriction.removed", nameof(MemberRestriction), restrictionId, before, restriction, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success((await GetByIdAsync(id, ct))!); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return MemberResult.Fail(MemberFailure.Conflict, ex.Message); }
    }
    public async Task<MemberResult> AddPaymentAsync(Guid id, Guid violationId, decimal amount, FinePaymentMethod method, string? reference, Guid? actor, CancellationToken ct) {
        var violation = await repository.GetViolationAsync(id, violationId, ct); if (violation is null) return MemberResult.Fail(MemberFailure.NotFound, "Violation was not found for this member.");
        var balance = await repository.GetViolationBalanceAsync(id, violationId, violation.FineAmount, ct); if (amount > balance) return MemberResult.Fail(MemberFailure.Validation, "Payment cannot exceed the outstanding balance.");
        try { var now = timeProvider.GetUtcNow().UtcDateTime; var payment = FinePayment.Create(id, violationId, amount, method, reference, now, actor); await repository.AddPaymentAsync(payment, ct); if (amount == balance && violation.IsOpen) violation.MarkPaid(now); await Audit("fine-payment.recorded", payment.Id, null, payment, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success((await GetByIdAsync(id, ct))!); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    public async Task<MemberResult> AddAdjustmentAsync(Guid id, Guid violationId, decimal delta, string reason, Guid? actor, CancellationToken ct) {
        var violation = await repository.GetViolationAsync(id, violationId, ct); if (violation is null) return MemberResult.Fail(MemberFailure.NotFound, "Violation was not found for this member.");
        var balance = await repository.GetViolationBalanceAsync(id, violationId, violation.FineAmount, ct); if (balance + delta < 0) return MemberResult.Fail(MemberFailure.Validation, "Adjustment cannot make the balance negative.");
        try { var now = timeProvider.GetUtcNow().UtcDateTime; var adjustment = FineAdjustment.Create(id, violationId, delta, reason, now, actor); await repository.AddAdjustmentAsync(adjustment, ct); if (balance + delta == 0 && violation.IsOpen) violation.MarkWaived(now); await Audit("fine-adjustment.recorded", adjustment.Id, null, adjustment, actor, now, ct); await repository.SaveChangesAsync(ct); return MemberResult.Success((await GetByIdAsync(id, ct))!); }
        catch (ArgumentException ex) { return MemberResult.Fail(MemberFailure.Validation, ex.Message); }
    }
    private async Task<MemberResult?> Validate(SaveMemberCommand c, Guid? id, CancellationToken ct) {
        if (await repository.CodeExistsAsync(c.MemberCode.Trim().ToUpperInvariant(), id, ct)) return MemberResult.Fail(MemberFailure.Conflict, "Member code already exists.");
        if (await repository.EmailExistsAsync(c.Email.Trim().ToLowerInvariant(), id, ct)) return MemberResult.Fail(MemberFailure.Conflict, "Member email already exists."); return null;
    }
    private Task Audit(string action, Guid id, string? before, object after, Guid? actor, DateTime now, CancellationToken ct) => Audit(action, nameof(Member), id, before, after, actor, now, ct);
    private Task Audit(string action, string entityType, Guid id, string? before, object after, Guid? actor, DateTime now, CancellationToken ct) => repository.AddAuditLogAsync(AuditLog.Create(actor, action, entityType, id, before, JsonSerializer.Serialize(after), now), ct);
    private static MemberResult ConcurrencyConflict() => MemberResult.Fail(MemberFailure.Conflict, "Member was modified by another request.");
    private static MemberModel MapSummary(Member m) => new(m.Id,m.MemberCode,m.FullName,m.Email,m.PhoneNumber,m.DateOfBirth,m.Address,m.MemberGroup,m.Status,m.BorrowingLimit,m.LoanPeriodDays,m.ConcurrencyToken,m.CreatedAtUtc,m.UpdatedAtUtc, m.MembershipCard is null ? null : new(m.MembershipCard.Id,m.MembershipCard.CardNumber,m.MembershipCard.IssuedOn,m.MembershipCard.ExpiresOn,m.MembershipCard.Status),[],[],[],[]);
    private async Task<MemberModel> MapDetails(Member m, CancellationToken ct) { var h = await repository.GetHistoryAsync(m.Id, ct); var now = timeProvider.GetUtcNow().UtcDateTime; var fines = h.Violations.Select(v => { var adjustments=h.Adjustments.Where(x=>x.ViolationId==v.Id).Sum(x=>x.AmountDelta); var payments=h.Payments.Where(x=>x.ViolationId==v.Id).Sum(x=>x.Amount); var balance=Math.Max(0,v.FineAmount+adjustments-payments); return new FineModel(v.Id,v.Type,v.BookTitle,v.Note,v.FineAmount,adjustments,payments,balance,v.RecordedAtUtc,v.IsOpen ? "open" : v.Resolution ?? "resolved"); }).ToArray(); var s=MapSummary(m); return s with { Restrictions = m.Restrictions.Select(r=>new RestrictionModel(r.Id,r.Type,r.Reason,r.StartsAtUtc,r.EndsAtUtc,r.RemovedAtUtc,r.RemovalReason,r.RemovedAtUtc is null && r.StartsAtUtc<=now && (r.EndsAtUtc is null || r.EndsAtUtc>now))).OrderByDescending(x=>x.StartsAtUtc).ToArray(), Borrowings=h.Borrowings.Select(x=>new BorrowingHistoryModel(x.Id,x.BookId,x.BorrowedAtUtc,x.DueAtUtc,x.ReturnedAtUtc)).ToArray(), Reservations=h.Reservations.Select(x=>new ReservationHistoryModel(x.Id,x.BookId,x.ReservedAtUtc,x.ExpiresAtUtc,x.FulfilledAtUtc,x.CancelledAtUtc)).ToArray(), Fines=fines }; }
}
