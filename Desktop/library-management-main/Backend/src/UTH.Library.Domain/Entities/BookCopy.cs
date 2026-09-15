using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class BookCopy
{
	private BookCopy() { }

	public static BookCopy Create(Guid bookId, string barcode, CopyCondition condition, Guid? shelfId, Guid? stockReceiptItemId, DateTime acquiredAtUtc)
	{
		if (bookId == Guid.Empty) throw new ArgumentException("Book is required.", nameof(bookId));
		return new BookCopy
		{
			Id = Guid.NewGuid(), BookId = bookId, Barcode = NormalizeBarcode(barcode), Condition = condition,
			Status = CopyStatus.Available, ShelfId = shelfId, StockReceiptItemId = stockReceiptItemId,
			AcquiredAtUtc = acquiredAtUtc, ConcurrencyToken = Guid.NewGuid()
		};
	}

	public Guid Id { get; private set; } public Guid BookId { get; private set; } public string Barcode { get; private set; } = string.Empty; public CopyCondition Condition { get; private set; } public CopyStatus Status { get; private set; } public DateTime AcquiredAtUtc { get; private set; } public Guid? ShelfId { get; private set; } public Guid? StockReceiptItemId { get; private set; } public Guid ConcurrencyToken { get; private set; }

	public void ChangeStatus(CopyStatus nextStatus)
	{
		if (!IsValidTransition(Status, nextStatus))
			throw new InvalidOperationException($"Book copy cannot move from {Status} to {nextStatus}.");
		Status = nextStatus;
		ConcurrencyToken = Guid.NewGuid();
	}

	public void Relocate(Guid shelfId)
	{
		if (shelfId == Guid.Empty) throw new ArgumentException("Shelf is required.", nameof(shelfId));
		if (Status is CopyStatus.Borrowed or CopyStatus.Lost or CopyStatus.Withdrawn)
			throw new InvalidOperationException("This copy cannot be relocated in its current state.");
		ShelfId = shelfId;
		ConcurrencyToken = Guid.NewGuid();
	}

	private static bool IsValidTransition(CopyStatus current, CopyStatus next) => current == next || current switch
	{
		CopyStatus.Available => next is CopyStatus.Borrowed or CopyStatus.Reserved or CopyStatus.InTransit or CopyStatus.Lost or CopyStatus.Damaged or CopyStatus.Withdrawn,
		CopyStatus.Borrowed => next is CopyStatus.Available or CopyStatus.Lost or CopyStatus.Damaged,
		CopyStatus.Reserved => next is CopyStatus.Available or CopyStatus.Borrowed or CopyStatus.InTransit,
		CopyStatus.InTransit => next is CopyStatus.Available or CopyStatus.Lost or CopyStatus.Damaged,
		CopyStatus.Damaged => next is CopyStatus.Available or CopyStatus.Withdrawn,
		CopyStatus.Lost => next is CopyStatus.Available or CopyStatus.Withdrawn,
		CopyStatus.Withdrawn => false,
		_ => false
	};

	private static string NormalizeBarcode(string barcode)
	{
		if (string.IsNullOrWhiteSpace(barcode)) throw new ArgumentException("Barcode is required.", nameof(barcode));
		var normalized = barcode.Trim().ToUpperInvariant();
		if (normalized.Length > 64) throw new ArgumentException("Barcode cannot exceed 64 characters.", nameof(barcode));
		return normalized;
	}
}
