namespace UTH.Library.Application.Features.Adjustments;

public enum AdjustmentType
{
    Decrease,
    Increase,
    Waive
}

public sealed record FineAdjustmentItemModel(
    Guid Id,
    Guid MemberId,
    Guid ViolationId,
    decimal AmountDelta,
    string Reason,
    DateTime AdjustedAtUtc,
    Guid? AdjustedByUserId);

public sealed record FineAdjustmentPreviewResult(
    Guid ViolationId,
    decimal OriginalFineAmount,
    decimal TotalAdjusted,
    decimal TotalPaid,
    decimal CurrentBalance,
    decimal AdjustmentAmountDelta,
    decimal ProjectedBalance,
    string ProjectedStatus,
    bool IsAllowed,
    string? ValidationMessage = null);

public sealed record CreateFineAdjustmentCommand(
    Guid ViolationId,
    decimal AmountDelta,
    string Reason,
    Guid? ActorUserId);

public sealed record WaiveViolationCommand(
    Guid ViolationId,
    string Reason,
    Guid? ActorUserId);

public enum FineAdjustmentFailure
{
    None,
    NotFound,
    Validation,
    Conflict
}

public sealed record FineAdjustmentResult(
    bool Succeeded,
    FineAdjustmentFailure Failure,
    FineAdjustmentItemModel? Adjustment,
    decimal NewBalance,
    string Status,
    IReadOnlyList<string> Errors)
{
    public static FineAdjustmentResult Success(FineAdjustmentItemModel adjustment, decimal newBalance, string status) =>
        new(true, FineAdjustmentFailure.None, adjustment, newBalance, status, []);

    public static FineAdjustmentResult Fail(FineAdjustmentFailure failure, params string[] errors) =>
        new(false, failure, null, 0m, string.Empty, errors);
}
