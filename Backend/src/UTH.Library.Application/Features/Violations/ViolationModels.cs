namespace UTH.Library.Application.Features.Violations;

public sealed record ViolationModel(
    Guid Id,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    Guid? BookId,
    string BookTitle,
    string Type,
    string Note,
    decimal FineAmount,
    DateTime RecordedAtUtc,
    DateTime? ResolvedAtUtc,
    string Status,
    Guid? AppliedPolicyId,
    int AppliedPolicyVersion,
    decimal TotalAdjusted = 0m,
    decimal TotalPaid = 0m,
    decimal Balance = 0m,
    Guid? BorrowingId = null,
    Guid? BookCopyId = null,
    string? BookCopyBarcode = null,
    string? BorrowerMemberCode = null,
    string? BorrowerCardNumber = null,
    Guid ConcurrencyToken = default);

public sealed record ViolationListQuery(
    string? Search,
    Guid? BorrowerId,
    string? Type,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    bool? HasBalanceOnly,
    int PageNumber,
    int PageSize);

public sealed record ViolationPageModel(
    IReadOnlyList<ViolationModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateViolationCommand(
    Guid BorrowerId,
    Guid? BookId,
    Guid? BookCopyId,
    Guid? BorrowingId,
    string Type,
    string Note,
    decimal FineAmount,
    int OverdueDays = 0,
    decimal BookPrice = 0,
    string? DamageLevel = null,
    Guid? ActorUserId = null);

public sealed record FinePreviewCommand(
    Guid BorrowerId,
    Guid? BookId,
    string Type,
    int OverdueDays = 0,
    decimal BookPrice = 0,
    string? DamageLevel = null,
    decimal? CustomAmount = null);

public sealed record FinePreviewResult(
    decimal CalculatedFine,
    string Formula,
    string? PolicyName,
    decimal DailyRate,
    decimal MaxFine,
    decimal LostRatio,
    Guid? PolicyId,
    int PolicyVersion);

public sealed record PaymentHistoryItemModel(
    Guid Id,
    decimal Amount,
    string Method,
    string Reference,
    DateTime PaidAtUtc,
    Guid? ReceivedByUserId);

public sealed record AdjustmentHistoryItemModel(
    Guid Id,
    decimal AmountDelta,
    string Reason,
    DateTime AdjustedAtUtc,
    Guid? AdjustedByUserId);

public sealed record ViolationDetailModel(
    ViolationModel Violation,
    string? BookIsbn,
    string? BookAuthor,
    string? BookCategory,
    string? CalculationBasis,
    IReadOnlyList<PaymentHistoryItemModel> Payments,
    IReadOnlyList<AdjustmentHistoryItemModel> Adjustments);

public enum ViolationFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record ViolationResult(
    bool Succeeded,
    ViolationFailure Failure,
    ViolationModel? Violation,
    IReadOnlyList<string> Errors)
{
    public static ViolationResult Success(ViolationModel violation) =>
        new(true, ViolationFailure.None, violation, []);

    public static ViolationResult Fail(ViolationFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
