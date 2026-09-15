using UTH.Library.Domain.Entities;
namespace UTH.Library.Application.Features.Members;

public sealed record MemberListQuery(string? Search, MemberStatus? Status, string? MemberGroup, int PageNumber, int PageSize);
public sealed record SaveMemberCommand(string MemberCode, string FullName, string Email, string? PhoneNumber,
    DateOnly? DateOfBirth, string? Address, string MemberGroup, MemberStatus Status, int BorrowingLimit,
    int LoanPeriodDays, Guid? ConcurrencyToken = null);
public sealed record CardModel(Guid Id, string CardNumber, DateOnly IssuedOn, DateOnly ExpiresOn, MembershipCardStatus Status);
public sealed record RestrictionModel(Guid Id, MemberRestrictionType Type, string Reason, DateTime StartsAtUtc,
    DateTime? EndsAtUtc, DateTime? RemovedAtUtc, string? RemovalReason, bool IsActive);
public sealed record BorrowingHistoryModel(Guid Id, Guid BookId, DateTime BorrowedAtUtc, DateTime DueAtUtc, DateTime? ReturnedAtUtc);
public sealed record ReservationHistoryModel(Guid Id, Guid BookId, DateTime ReservedAtUtc, DateTime ExpiresAtUtc, DateTime? FulfilledAtUtc, DateTime? CancelledAtUtc);
public sealed record FineModel(Guid Id, string Type, string BookTitle, string Note, decimal OriginalAmount,
    decimal AdjustmentTotal, decimal PaymentTotal, decimal Balance, DateTime RecordedAtUtc, string Status);
public sealed record MemberModel(Guid Id, string MemberCode, string FullName, string Email, string? PhoneNumber,
    DateOnly? DateOfBirth, string? Address, string MemberGroup, MemberStatus Status, int BorrowingLimit,
    int LoanPeriodDays, Guid ConcurrencyToken, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, CardModel? Card,
    IReadOnlyList<RestrictionModel> Restrictions, IReadOnlyList<BorrowingHistoryModel> Borrowings,
    IReadOnlyList<ReservationHistoryModel> Reservations, IReadOnlyList<FineModel> Fines);
public sealed record MemberPage(IReadOnlyList<MemberModel> Items, int PageNumber, int PageSize, int TotalCount);
public enum MemberFailure { None, NotFound, Conflict, Validation }
public sealed record MemberResult(bool Succeeded, MemberFailure Failure, MemberModel? Member, IReadOnlyList<string> Errors) {
    public static MemberResult Success(MemberModel member) => new(true, MemberFailure.None, member, []);
    public static MemberResult Fail(MemberFailure failure, string error) => new(false, failure, null, [error]);
}
