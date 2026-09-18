using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Borrowings;

public sealed class BorrowingFilterRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateBorrowingRequest(
    [Required] Guid BookId,
    [Required] Guid BorrowerId,
    [Range(1, 365)] int LoanDays = 14);

public sealed record CheckoutWithBarcodeRequest(
    [Required] string MemberCardOrCode,
    [Required] string BookBarcode,
    [Range(1, 365)] int? LoanDaysOverride = null);

public sealed record RenewBorrowingRequest([Required] Guid ConcurrencyToken);

public sealed record MemberCheckoutLookupResponse(
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

public sealed record BookCopyCheckoutLookupResponse(
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

public sealed record BorrowingResponse(
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

public sealed record BorrowingPageResponse(
    IReadOnlyCollection<BorrowingResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record BookCopyReturnLookupResponse(
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

public sealed record ConfirmReturnRequest(
    string Barcode,
    string Condition,
    string? Note,
    decimal? CustomDamageFine,
    decimal? CustomLostFine,
    Guid ConcurrencyToken);

public sealed record ViolationSummaryResponse(
    Guid Id,
    string Type,
    string Note,
    decimal FineAmount,
    DateTime RecordedAtUtc);

public sealed record ReturnExecutionResponse(
    BorrowingResponse Borrowing,
    DateTime ReturnedAtUtc,
    string Condition,
    string Status,
    IReadOnlyList<ViolationSummaryResponse> Violations,
    decimal TotalFine,
    bool HasWaitingReservation);
