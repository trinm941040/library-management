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

    private static BookCopy CreateBase(Guid bookId, string barcode, DateTime acquiredAtUtc)
    {
        if (bookId == Guid.Empty) throw new ArgumentException("Book is required.", nameof(bookId));
        return new BookCopy(Guid.NewGuid(), bookId, NormalizeBarcode(barcode), EnsureUtc(acquiredAtUtc));
    }

    public static BookCopy Create(Guid bookId, string barcode, CopyCondition condition, Guid shelfId, Guid? stockReceiptItemId, DateTime acquiredAtUtc)
    {
        if (!Enum.IsDefined(condition)) throw new ArgumentOutOfRangeException(nameof(condition));
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf is required.", nameof(shelfId));
        var copy = CreateBase(bookId, barcode, acquiredAtUtc);
        copy.Condition = condition;
        copy.ShelfId = shelfId;
        copy.StockReceiptItemId = stockReceiptItemId;
        return copy;
    }

    public static string NormalizeBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) throw new ArgumentException("Barcode is required.", nameof(barcode));
        var normalized = barcode.Trim().ToUpperInvariant();
        if (normalized.Length > 64) throw new ArgumentException("Barcode cannot exceed 64 characters.", nameof(barcode));
        if (normalized.Any(char.IsControl)) throw new ArgumentException("Barcode contains invalid characters.", nameof(barcode));
        return normalized;
    }

    public void ChangeStatus(CopyStatus nextStatus)
    {
        if (!Enum.IsDefined(nextStatus) || !CanChangeStatus(nextStatus))
            throw new InvalidOperationException($"Book copy cannot move from {Status} to {nextStatus}.");
        if (Status == nextStatus) return;
        Status = nextStatus;
        ConcurrencyToken = Guid.NewGuid();
    }

    public bool CanChangeStatus(CopyStatus nextStatus) => Status == nextStatus || Status switch
    {
        CopyStatus.Available => nextStatus is CopyStatus.Borrowed or CopyStatus.Reserved or CopyStatus.InTransit or CopyStatus.Lost or CopyStatus.Damaged or CopyStatus.Withdrawn,
        CopyStatus.Borrowed => nextStatus is CopyStatus.Available or CopyStatus.Lost or CopyStatus.Damaged,
        CopyStatus.Reserved => nextStatus is CopyStatus.Available or CopyStatus.Borrowed or CopyStatus.InTransit,
        CopyStatus.InTransit => nextStatus is CopyStatus.Available or CopyStatus.Lost or CopyStatus.Damaged,
        CopyStatus.Lost => nextStatus is CopyStatus.Available or CopyStatus.Withdrawn,
        CopyStatus.Damaged => nextStatus is CopyStatus.Available or CopyStatus.Withdrawn,
        _ => false
    };

    public void Relocate(Guid shelfId)
    {
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf is required.", nameof(shelfId));
        if (Status is CopyStatus.Borrowed or CopyStatus.Lost or CopyStatus.Withdrawn)
            throw new InvalidOperationException("This copy cannot be relocated in its current state.");
        if (ShelfId == shelfId) return;
        ShelfId = shelfId;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Withdraw()
    {
        ChangeStatus(CopyStatus.Withdrawn);
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
