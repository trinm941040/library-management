namespace UTH.Library.Application.Features.Borrowings;

public sealed record BorrowingModel(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    string Status,
    int RenewalCount,
    Guid? AppliedPolicyId,
    int AppliedPolicyVersion,
    Guid? BookCopyId = null,
    string? BookCopyBarcode = null,
    Guid? ProcessedByEmployeeId = null);

public sealed record BorrowingListQuery(string? Search, string? Status, int PageNumber, int PageSize);

public sealed record BorrowingPageModel(
    IReadOnlyList<BorrowingModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateBorrowingCommand(Guid BookId, Guid BorrowerId, int LoanDays);
public sealed record CheckoutWithBarcodeCommand(string MemberCardOrCode, string BookBarcode, int? LoanDaysOverride);
public sealed record RenewBorrowingCommand(Guid ActorUserId, Guid ConcurrencyToken);

public sealed record MemberCheckoutLookupResult(
    Guid MemberId,
    string MemberCode,
    string FullName,
    string Email,
    string MemberGroup,
    string? CardNumber,
    DateOnly? CardExpiresOn,
    string? CardStatus,
    string MemberStatus,
    int ActiveBorrowingsCount,
    int BorrowingLimit,
    int OverdueLoansCount,
    bool IsEligible,
    IReadOnlyList<string> IneligibilityReasons);

public sealed record BookCopyCheckoutLookupResult(
    Guid CopyId,
    Guid BookId,
    string Barcode,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string Condition,
    string Status,
    bool IsAvailable,
    string? IneligibilityReason,
    string? PolicyName,
    int LoanPeriodDays,
    DateTime? SampleDueAtUtc);

public enum BorrowingFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record BorrowingResult(
    bool Succeeded,
    BorrowingFailure Failure,
    BorrowingModel? Borrowing,
    IReadOnlyList<string> Errors)
{
    public static BorrowingResult Success(BorrowingModel borrowing) => new(true, BorrowingFailure.None, borrowing, []);
    public static BorrowingResult Fail(BorrowingFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}

public sealed record BookCopyReturnLookupResult(
    Guid BorrowingId,
    Guid BookId,
    string Title,
    string Author,
    Guid CopyId,
    string Barcode,
    string Condition,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    string? MemberCode,
    string? CardNumber,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    bool IsOverdue,
    int OverdueDays,
    decimal FinePerDay,
    decimal EstimatedOverdueFine,
    decimal FixedDamageFine,
    decimal EstimatedLostFine,
    string? PolicyName,
    Guid ConcurrencyToken);

public sealed record ConfirmReturnCommand(
    string Barcode,
    string Condition,
    string? Note,
    decimal? CustomDamageFine,
    decimal? CustomLostFine,
    Guid ConcurrencyToken);

public sealed record ViolationSummaryModel(
    Guid Id,
    string Type,
    string Note,
    decimal FineAmount,
    DateTime RecordedAtUtc);

public sealed record ReturnExecutionResult(
    BorrowingModel Borrowing,
    DateTime ReturnedAtUtc,
    string Condition,
    string Status,
    IReadOnlyList<ViolationSummaryModel> Violations,
    decimal TotalFine,
    bool HasWaitingReservation);

public sealed record ReturnResult(
    bool Succeeded,
    BorrowingFailure Failure,
    ReturnExecutionResult? Result,
    IReadOnlyList<string> Errors)
{
    public static ReturnResult Success(ReturnExecutionResult result) => new(true, BorrowingFailure.None, result, []);
    public static ReturnResult Fail(BorrowingFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
