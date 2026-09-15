using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class StockReceipt
{
	private StockReceipt() { }
	public static StockReceipt Create(string receiptNumber, Guid supplierId, Guid branchId, Guid receivedByUserId, DateTime receivedAtUtc, string? notes)
	{
		if (supplierId == Guid.Empty || branchId == Guid.Empty || receivedByUserId == Guid.Empty) throw new ArgumentException("Supplier, branch and receiver are required.");
		return new StockReceipt { Id = Guid.NewGuid(), ReceiptNumber = Required(receiptNumber, 50), SupplierId = supplierId, BranchId = branchId, ReceivedByUserId = receivedByUserId, Status = StockReceiptStatus.Draft, ReceivedAtUtc = receivedAtUtc, Notes = Optional(notes, 2000), ConcurrencyToken = Guid.NewGuid() };
	}
	public Guid Id { get; private set; } public string ReceiptNumber { get; private set; } = string.Empty; public Guid SupplierId { get; private set; } public Guid BranchId { get; private set; } public Guid ReceivedByUserId { get; private set; } public StockReceiptStatus Status { get; private set; } public DateTime ReceivedAtUtc { get; private set; } public DateTime? ConfirmedAtUtc { get; private set; } public string? Notes { get; private set; } public Guid ConcurrencyToken { get; private set; }
	public ICollection<StockReceiptItem> Items { get; private set; } = new List<StockReceiptItem>();
	public void Update(Guid supplierId, Guid branchId, DateTime receivedAtUtc, string? notes)
	{ if (Status is not (StockReceiptStatus.Draft or StockReceiptStatus.Received)) throw new InvalidOperationException("Only draft or receiving receipts can be edited."); SupplierId = supplierId; BranchId = branchId; ReceivedAtUtc = receivedAtUtc; Notes = Optional(notes, 2000); ConcurrencyToken = Guid.NewGuid(); }
	private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException("Receipt number is invalid."); return value.Trim(); }
	private static string? Optional(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); if (result.Length > max) throw new ArgumentException("Receipt notes are invalid."); return result; }
}
