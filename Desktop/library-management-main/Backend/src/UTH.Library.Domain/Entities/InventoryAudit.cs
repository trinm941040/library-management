using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class InventoryAudit
{
	private InventoryAudit() { }
	public static InventoryAudit Create(Guid branchId, Guid startedByUserId, string? notes, DateTime startedAtUtc) => new() { Id = Guid.NewGuid(), BranchId = branchId, StartedByUserId = startedByUserId, Status = InventoryAuditStatus.InProgress, StartedAtUtc = startedAtUtc, Notes = notes, ConcurrencyToken = Guid.NewGuid() };
	public Guid Id { get; private set; } public Guid BranchId { get; private set; } public Guid StartedByUserId { get; private set; } public InventoryAuditStatus Status { get; private set; } public DateTime StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public string? Notes { get; private set; } public Guid ConcurrencyToken { get; private set; }
	public void Complete(DateTime completedAtUtc) { if (Status != InventoryAuditStatus.InProgress) throw new InvalidOperationException("Only an active audit can be completed."); Status = InventoryAuditStatus.Completed; CompletedAtUtc = completedAtUtc; ConcurrencyToken = Guid.NewGuid(); }
}
