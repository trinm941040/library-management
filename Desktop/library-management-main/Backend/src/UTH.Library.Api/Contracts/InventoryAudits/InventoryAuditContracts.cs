using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;
namespace UTH.Library.Api.Contracts.InventoryAudits;
public sealed record CreateInventoryAuditRequest(Guid BranchId, Guid? AreaId, Guid? ShelfId, string? Notes);
public sealed record ScanInventoryAuditRequest(string Barcode, Guid? ActualShelfId, AuditItemResult Result, Guid? ConcurrencyToken = null);
public sealed record CompleteInventoryAuditRequest(Guid? ConcurrencyToken = null);
public sealed record InventoryAuditItemResponse(Guid Id, Guid BookCopyId, string Barcode, Guid? ExpectedShelfId, Guid? ActualShelfId, AuditItemResult Result, DateTime? ScannedAtUtc);
public sealed record InventoryAuditResponse(Guid Id, Guid BranchId, InventoryAuditStatus Status, DateTime StartedAtUtc, DateTime? CompletedAtUtc, Guid ConcurrencyToken, int TotalItems, int PendingItems, int FoundItems, int MissingItems, int MisplacedItems, IReadOnlyCollection<InventoryAuditItemResponse> Items);
