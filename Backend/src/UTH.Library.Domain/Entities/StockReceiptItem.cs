namespace UTH.Library.Domain.Entities;

public sealed class StockReceiptItem
{
    private StockReceiptItem() { }

    public Guid Id { get; private set; }
    public Guid StockReceiptId { get; private set; }
    public Guid BookId { get; private set; }
    public int ExpectedQuantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public int DamagedQuantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static StockReceiptItem Create(Guid receiptId, Guid bookId, int expectedQuantity,
        int receivedQuantity, int damagedQuantity, decimal? unitCost)
    {
        if (receiptId == Guid.Empty || bookId == Guid.Empty)
            throw new ArgumentException("Phiếu nhập và sách là bắt buộc.");
        var item = new StockReceiptItem { Id = Guid.NewGuid(), StockReceiptId = receiptId, BookId = bookId };
        item.Update(expectedQuantity, receivedQuantity, damagedQuantity, unitCost);
        return item;
    }

    public void Update(int expectedQuantity, int receivedQuantity, int damagedQuantity, decimal? unitCost)
    {
        if (expectedQuantity < 0 || receivedQuantity < 0 || damagedQuantity < 0 ||
            damagedQuantity > receivedQuantity)
            throw new ArgumentException("Số lượng phải không âm; số hỏng không được vượt thực nhận.");
        if (unitCost is < 0 or > 9999999999999999.99m ||
            (unitCost is not null && decimal.Round(unitCost.Value, 2) != unitCost.Value))
            throw new ArgumentException("Đơn giá phải không âm và có tối đa 2 chữ số thập phân.");
        ExpectedQuantity = expectedQuantity;
        ReceivedQuantity = receivedQuantity;
        DamagedQuantity = damagedQuantity;
        UnitCost = unitCost;
        ConcurrencyToken = Guid.NewGuid();
    }
}
