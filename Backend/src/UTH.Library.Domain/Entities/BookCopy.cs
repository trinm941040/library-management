using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class BookCopy
{
    private BookCopy(Guid id, Guid bookId, string barcode, DateTime acquiredAtUtc)
    {
        Id = id;
        BookId = bookId;
        Barcode = barcode;
        Condition = CopyCondition.Good;
        Status = CopyStatus.Available;
        AcquiredAtUtc = acquiredAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    private BookCopy() { }

    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public string Barcode { get; private set; } = string.Empty;
    public CopyCondition Condition { get; private set; }
    public CopyStatus Status { get; private set; }
    public DateTime AcquiredAtUtc { get; private set; }
    public Guid? ShelfId { get; private set; }
    public Guid? StockReceiptItemId { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static BookCopy Create(Guid bookId, string barcode, DateTime acquiredAtUtc)
    {
        if (bookId == Guid.Empty) throw new ArgumentException("Book is required.", nameof(bookId));
        if (string.IsNullOrWhiteSpace(barcode)) throw new ArgumentException("Barcode is required.", nameof(barcode));
        return new BookCopy(Guid.NewGuid(), bookId, barcode.Trim(), acquiredAtUtc);
    }

    public void Withdraw()
    {
        if (Status != CopyStatus.Available)
            throw new InvalidOperationException("Only an available copy can be withdrawn.");
        Status = CopyStatus.Withdrawn;
        ConcurrencyToken = Guid.NewGuid();
    }
}
