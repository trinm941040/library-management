using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Payments;

public sealed record FinePaymentPreviewResult(
    Guid ViolationId,
    string ViolationType,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    string? MemberCode,
    string BorrowerEmail,
    decimal FineAmount,
    decimal TotalAdjusted,
    decimal TotalPaid,
    decimal Balance,
    decimal SuggestedAmount,
    bool IsOpen,
    string Status);

public sealed record CreateFinePaymentCommand(
    Guid ViolationId,
    decimal Amount,
    FinePaymentMethod Method,
    string? Reference,
    string? IdempotencyKey,
    Guid? ActorUserId,
    string? ActorUserName = null);

public sealed record FinePaymentReceiptModel(
    Guid Id,
    Guid ViolationId,
    string ViolationType,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    string? MemberCode,
    decimal Amount,
    decimal PreviousBalance,
    decimal RemainingBalance,
    string Method,
    string Reference,
    DateTime PaidAtUtc,
    Guid? ReceivedByUserId,
    string? ReceivedByUserName,
    bool IsFullyPaid);

public sealed record FinePaymentListQuery(
    string? Search,
    Guid? ViolationId,
    Guid? MemberId,
    FinePaymentMethod? Method,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber,
    int PageSize);

public sealed record FinePaymentPageModel(
    IReadOnlyList<FinePaymentReceiptModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public enum FinePaymentFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record FinePaymentResult(
    bool Succeeded,
    FinePaymentFailure Failure,
    FinePaymentReceiptModel? Receipt,
    IReadOnlyList<string> Errors)
{
    public static FinePaymentResult Success(FinePaymentReceiptModel receipt) =>
        new(true, FinePaymentFailure.None, receipt, []);

    public static FinePaymentResult Fail(FinePaymentFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
