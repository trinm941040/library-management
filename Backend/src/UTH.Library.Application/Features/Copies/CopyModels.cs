using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Copies;

public sealed record CopyModel(Guid Id, Guid BookId, string BookTitle, string Barcode, CopyCondition Condition,
    CopyStatus Status, DateTime AcquiredAtUtc, Guid? ShelfId, string? ShelfCode, Guid? BranchId,
    string? BranchCode, Guid? StockReceiptItemId, Guid ConcurrencyToken);

public sealed record CreateCopyCommand(Guid BookId, string Barcode, CopyCondition Condition,
    Guid ShelfId, Guid? StockReceiptItemId);

public sealed record ChangeCopyStatusCommand(CopyStatus Status, Guid ConcurrencyToken);
public sealed record RelocateCopyCommand(Guid ShelfId, Guid ConcurrencyToken);
