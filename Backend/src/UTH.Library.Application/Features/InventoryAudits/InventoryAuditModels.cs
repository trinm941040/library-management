using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.InventoryAudits;

public sealed record CreateInventoryAuditCommand(Guid BranchId, Guid? AreaId, Guid? ShelfId, string? Notes);
public sealed record ScanInventoryAuditCommand(string Barcode, Guid ActualShelfId,
    CopyStatus ActualStatus, CopyCondition ActualCondition, Guid ConcurrencyToken);
public sealed record CompleteInventoryAuditCommand(Guid ConcurrencyToken, bool AcknowledgeDiscrepancies);
public sealed record ApplyInventoryCorrectionCommand(Guid BookCopyId, Guid ConcurrencyToken,
    Guid? ShelfId, CopyStatus? Status, CopyCondition? Condition);
public sealed record ApplyInventoryCorrectionsCommand(IReadOnlyList<ApplyInventoryCorrectionCommand> Corrections);
public sealed record InventoryAuditItemModel(Guid Id, Guid BookCopyId, Guid CopyConcurrencyToken,
    string Barcode, string BookTitle,
    bool IsExpected, Guid? ExpectedShelfId, Guid? ActualShelfId,
    CopyStatus ExpectedStatus, CopyStatus? ActualStatus,
    CopyCondition ExpectedCondition, CopyCondition? ActualCondition,
    AuditItemResult Result, DateTime? ScannedAtUtc);
public sealed record InventoryAuditModel(Guid Id, Guid BranchId, Guid? AreaId, Guid? ShelfId,
    Guid StartedByUserId, InventoryAuditStatus Status, DateTime StartedAtUtc,
    DateTime? CompletedAtUtc, string? Notes, Guid ConcurrencyToken,
    int ExpectedCount, int ScannedCount, int PendingCount, int FoundCount,
    int DiscrepancyCount, IReadOnlyList<InventoryAuditItemModel> Items);
