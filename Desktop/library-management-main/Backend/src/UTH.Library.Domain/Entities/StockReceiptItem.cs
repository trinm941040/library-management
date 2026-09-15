namespace UTH.Library.Domain.Entities;
public sealed class StockReceiptItem
{
	private StockReceiptItem() { }
	public static StockReceiptItem Create(Guid receiptId, Guid bookId, int expectedQuantity, int receivedQuantity, int damagedQuantity, decimal? unitCost) { Validate(expectedQuantity, receivedQuantity, damagedQuantity, unitCost); return new StockReceiptItem { Id = Guid.NewGuid(), StockReceiptId = receiptId, BookId = bookId, ExpectedQuantity = expectedQuantity, ReceivedQuantity = receivedQuantity, DamagedQuantity = damagedQuantity, UnitCost = unitCost, ConcurrencyToken = Guid.NewGuid() }; }
	public Guid Id { get; private set; } public Guid StockReceiptId { get; private set; } public Guid BookId { get; private set; } public int ExpectedQuantity { get; private set; } public int ReceivedQuantity { get; private set; } public int DamagedQuantity { get; private set; } public decimal? UnitCost { get; private set; } public Guid ConcurrencyToken { get; private set; }
	public void Update(int expectedQuantity, int receivedQuantity, int damagedQuantity, decimal? unitCost) { Validate(expectedQuantity, receivedQuantity, damagedQuantity, unitCost); ExpectedQuantity = expectedQuantity; ReceivedQuantity = receivedQuantity; DamagedQuantity = damagedQuantity; UnitCost = unitCost; ConcurrencyToken = Guid.NewGuid(); }
	private static void Validate(int expected, int received, int damaged, decimal? cost) { if (expected < 0 || received < 0 || damaged < 0) throw new ArgumentOutOfRangeException(nameof(expected), "Quantities cannot be negative."); if (received + damaged > expected) throw new ArgumentException("Received and damaged quantities cannot exceed expected quantity."); if (cost is < 0) throw new ArgumentOutOfRangeException(nameof(cost), "Unit cost cannot be negative."); }
}
