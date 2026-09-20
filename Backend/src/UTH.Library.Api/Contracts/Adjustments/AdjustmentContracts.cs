namespace UTH.Library.Api.Contracts.Adjustments;

public sealed record FineAdjustmentPreviewRequest(decimal AmountDelta);

public sealed record FineAdjustmentPreviewResponse(
    Guid ViolationId,
    decimal OriginalFineAmount,
    decimal TotalAdjusted,
    decimal TotalPaid,
    decimal CurrentBalance,
    decimal AdjustmentAmountDelta,
    decimal ProjectedBalance,
    string ProjectedStatus,
    bool IsAllowed,
    string? ValidationMessage);

public sealed record CreateFineAdjustmentRequest(
    decimal AmountDelta,
    string Reason);

public sealed record WaiveViolationRequest(
    string Reason);

public sealed record FineAdjustmentResponse(
    Guid Id,
    Guid MemberId,
    Guid ViolationId,
    decimal AmountDelta,
    string Reason,
    DateTime AdjustedAtUtc,
    Guid? AdjustedByUserId);

public sealed record FineAdjustmentResultResponse(
    bool Succeeded,
    FineAdjustmentResponse? Adjustment,
    decimal NewBalance,
    string Status,
    IReadOnlyList<string> Errors);
