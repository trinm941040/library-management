using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class InventoryAuditItem { public Guid Id { get; private set; } public Guid InventoryAuditId { get; private set; } public Guid BookCopyId { get; private set; } public Guid? ExpectedShelfId { get; private set; } public Guid? ActualShelfId { get; private set; } public AuditItemResult Result { get; private set; } public DateTime? ScannedAtUtc { get; private set; } public Guid ConcurrencyToken { get; private set; } }
