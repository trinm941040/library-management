using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class BookCopy
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public string Barcode { get; private set; } = string.Empty;
    public CopyCondition Condition { get; private set; }
    public CopyStatus Status { get; private set; }
    public DateTime AcquiredAtUtc { get; private set; }
    public Guid? ShelfId { get; private set; }
    public Guid? StockReceiptItemId { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static BookCopy Create(
        Guid bookId,
        string barcode,
        CopyCondition condition,
        CopyStatus status,
        DateTime acquiredAtUtc,
        Guid? shelfId = null,
        Guid? stockReceiptItemId = null)
    {
        if (bookId == Guid.Empty) throw new ArgumentException("Book ID is required.", nameof(bookId));
        if (string.IsNullOrWhiteSpace(barcode)) throw new ArgumentException("Barcode is required.", nameof(barcode));

        return new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Barcode = barcode.Trim().ToUpperInvariant(),
            Condition = condition,
            Status = status,
            AcquiredAtUtc = acquiredAtUtc,
            ShelfId = shelfId,
            StockReceiptItemId = stockReceiptItemId,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    public void Checkout(DateTime now)
    {
        if (Status != CopyStatus.Available)
            throw new InvalidOperationException($"Bản sao sách '{Barcode}' không khả dụng (Trạng thái: {Status}).");

        Status = CopyStatus.Borrowed;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Return(DateTime now)
    {
        Status = CopyStatus.Available;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void ReturnWithCondition(CopyCondition condition, CopyStatus status, DateTime now)
    {
        Condition = condition;
        Status = status;
        ConcurrencyToken = Guid.NewGuid();
    }
}
