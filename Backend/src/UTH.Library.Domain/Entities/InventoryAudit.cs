using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class InventoryAudit { public Guid Id { get; private set; } public Guid BranchId { get; private set; } public Guid StartedByUserId { get; private set; } public InventoryAuditStatus Status { get; private set; } public DateTime StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public string? Notes { get; private set; } public Guid ConcurrencyToken { get; private set; } }
