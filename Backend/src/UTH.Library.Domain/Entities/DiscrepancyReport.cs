using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class DiscrepancyReport
{
    private DiscrepancyReport() { }

    public Guid Id { get; private set; }
    public Guid StockReceiptId { get; private set; }
    public DiscrepancyType Type { get; private set; }
    public int ExpectedQuantity { get; private set; }
    public int ActualQuantity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public static DiscrepancyReport Create(Guid receiptId, DiscrepancyType type, int expected,
        int actual, string description, DateTime createdAtUtc, Guid actorId)
    {
        if (receiptId == Guid.Empty || actorId == Guid.Empty || expected < 0 || actual < 0)
            throw new ArgumentException("Thông tin sai lệch không hợp lệ.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000)
            throw new ArgumentException("Mô tả sai lệch là bắt buộc và tối đa 2000 ký tự.", nameof(description));
        return new DiscrepancyReport { Id = Guid.NewGuid(), StockReceiptId = receiptId,
            Type = type, ExpectedQuantity = expected, ActualQuantity = actual,
            Description = description.Trim(), CreatedAtUtc = createdAtUtc.Kind == DateTimeKind.Utc
                ? createdAtUtc : createdAtUtc.ToUniversalTime(), CreatedByUserId = actorId };
    }
}
