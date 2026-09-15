using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class BookCopy { public Guid Id { get; private set; } public Guid BookId { get; private set; } public string Barcode { get; private set; } = string.Empty; public CopyCondition Condition { get; private set; } public CopyStatus Status { get; private set; } public DateTime AcquiredAtUtc { get; private set; } public Guid? ShelfId { get; private set; } public Guid? StockReceiptItemId { get; private set; } public Guid ConcurrencyToken { get; private set; } }
