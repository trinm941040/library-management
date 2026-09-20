using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.InventoryAudits;

public sealed class InventoryAuditFilterRequest
{
    public Guid? BranchId { get; init; }
    public InventoryAuditStatus? Status { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record CreateInventoryAuditRequest(Guid BranchId, Guid? AreaId, Guid? ShelfId,
    [StringLength(2000)] string? Notes);
public sealed record ScanInventoryAuditRequest([Required] string Barcode, Guid ActualShelfId,
    CopyStatus ActualStatus, CopyCondition ActualCondition, Guid ConcurrencyToken);
public sealed record CompleteInventoryAuditRequest(Guid ConcurrencyToken,
    bool AcknowledgeDiscrepancies);
public sealed record ApplyInventoryCorrectionRequest(Guid BookCopyId, Guid ConcurrencyToken,
    Guid? ShelfId, CopyStatus? Status, CopyCondition? Condition);
public sealed record ApplyInventoryCorrectionsRequest(
    [Required] IReadOnlyList<ApplyInventoryCorrectionRequest> Corrections);
