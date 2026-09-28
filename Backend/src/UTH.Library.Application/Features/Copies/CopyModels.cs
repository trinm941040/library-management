using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Copies;

public sealed record CopyModel(Guid Id, Guid BookId, string BookTitle, string Barcode, CopyCondition Condition,
    CopyStatus Status, DateTime AcquiredAtUtc, Guid? ShelfId, string? ShelfCode, Guid? BranchId,
    string? BranchCode, Guid? StockReceiptItemId, Guid ConcurrencyToken);

public sealed record CreateCopyCommand(Guid BookId, string Barcode, CopyCondition Condition,
    Guid ShelfId, Guid? StockReceiptItemId);

public sealed record ChangeCopyStatusCommand(CopyStatus Status, Guid ConcurrencyToken);
public sealed record RelocateCopyCommand(Guid ShelfId, Guid ConcurrencyToken);
public sealed record ChangeCopyConditionCommand(CopyCondition Condition, Guid ConcurrencyToken);
public sealed record WithdrawCopyCommand(Guid ConcurrencyToken, string Reason);
public sealed record CopyOperationRow(Guid CopyId, Guid ConcurrencyToken, CopyStatus? Status, CopyCondition? Condition, Guid? ShelfId, string? Reason);
public sealed record CopyOperationResult(Guid CopyId, bool Succeeded, string? Error, CopyModel? Copy);
public sealed record ImportCopyRow(string Barcode, string Isbn, string ShelfCode, CopyCondition Condition);
public sealed record ImportCopyPreviewRow(int RowNumber, string Barcode, bool Valid, string? Error);
