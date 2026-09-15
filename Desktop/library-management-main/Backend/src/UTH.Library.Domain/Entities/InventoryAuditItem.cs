using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class InventoryAuditItem
{
	private InventoryAuditItem() { }
	public static InventoryAuditItem Create(Guid auditId, Guid copyId, Guid? expectedShelfId) => new() { Id = Guid.NewGuid(), InventoryAuditId = auditId, BookCopyId = copyId, ExpectedShelfId = expectedShelfId, Result = AuditItemResult.Pending, ConcurrencyToken = Guid.NewGuid() };
	public Guid Id { get; private set; } public Guid InventoryAuditId { get; private set; } public Guid BookCopyId { get; private set; } public Guid? ExpectedShelfId { get; private set; } public Guid? ActualShelfId { get; private set; } public AuditItemResult Result { get; private set; } public DateTime? ScannedAtUtc { get; private set; } public Guid ConcurrencyToken { get; private set; }
	public void Scan(Guid? actualShelfId, AuditItemResult result, DateTime scannedAtUtc) { if (Result != AuditItemResult.Pending) throw new InvalidOperationException("This copy was already scanned."); ActualShelfId = actualShelfId; Result = result; ScannedAtUtc = scannedAtUtc; ConcurrencyToken = Guid.NewGuid(); }
}
