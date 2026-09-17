using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class StockReceipt
{
    private StockReceipt() { }

    public Guid Id { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid ReceivedByUserId { get; private set; }
    public StockReceiptStatus Status { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static StockReceipt Create(string receiptNumber, Guid supplierId, Guid branchId,
        Guid receivedByUserId, DateTime receivedAtUtc, string? notes)
    {
        if (receivedByUserId == Guid.Empty) throw new ArgumentException("Người tiếp nhận không hợp lệ.", nameof(receivedByUserId));
        if (string.IsNullOrWhiteSpace(receiptNumber) || receiptNumber.Trim().Length > 50)
            throw new ArgumentException("Số phiếu nhập không hợp lệ.", nameof(receiptNumber));
        var receipt = new StockReceipt
        {
            Id = Guid.NewGuid(), ReceiptNumber = receiptNumber.Trim().ToUpperInvariant(),
            ReceivedByUserId = receivedByUserId, Status = StockReceiptStatus.Draft
        };
        receipt.Update(supplierId, branchId, receivedAtUtc, notes);
        return receipt;
    }

    public void Update(Guid supplierId, Guid branchId, DateTime receivedAtUtc, string? notes)
    {
        if (Status is not (StockReceiptStatus.Draft or StockReceiptStatus.Received))
            throw new InvalidOperationException("Chỉ có thể sửa phiếu nháp hoặc đang tiếp nhận.");
        if (supplierId == Guid.Empty) throw new ArgumentException("Nhà cung cấp không hợp lệ.", nameof(supplierId));
        if (branchId == Guid.Empty) throw new ArgumentException("Chi nhánh không hợp lệ.", nameof(branchId));
        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalizedNotes?.Length > 2000) throw new ArgumentException("Ghi chú quá dài.", nameof(notes));
        SupplierId = supplierId;
        BranchId = branchId;
        ReceivedAtUtc = receivedAtUtc.Kind switch
        {
            DateTimeKind.Utc => receivedAtUtc,
            DateTimeKind.Local => receivedAtUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc)
        };
        Notes = normalizedNotes;
        ConcurrencyToken = Guid.NewGuid();
    }
}
