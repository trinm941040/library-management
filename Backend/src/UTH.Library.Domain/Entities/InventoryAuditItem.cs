using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class InventoryAuditItem
{
    private InventoryAuditItem() { }

    public Guid Id { get; private set; }
    public Guid InventoryAuditId { get; private set; }
    public Guid BookCopyId { get; private set; }
    public bool IsExpected { get; private set; }
    public Guid? ExpectedShelfId { get; private set; }
    public Guid? ActualShelfId { get; private set; }
    public CopyStatus ExpectedStatus { get; private set; }
    public CopyStatus? ActualStatus { get; private set; }
    public CopyCondition ExpectedCondition { get; private set; }
    public CopyCondition? ActualCondition { get; private set; }
    public AuditItemResult Result { get; private set; }
    public DateTime? ScannedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static InventoryAuditItem CreateExpected(Guid auditId, BookCopy copy)
    {
        if (auditId == Guid.Empty || copy.Id == Guid.Empty) throw new ArgumentException("Đợt kiểm kê và bản sao là bắt buộc.");
        return new InventoryAuditItem { Id = Guid.NewGuid(), InventoryAuditId = auditId,
            BookCopyId = copy.Id, IsExpected = true, ExpectedShelfId = copy.ShelfId,
            ExpectedStatus = copy.Status, ExpectedCondition = copy.Condition,
            Result = AuditItemResult.Pending, ConcurrencyToken = Guid.NewGuid() };
    }

    public static InventoryAuditItem CreateUnexpected(Guid auditId, BookCopy copy, Guid actualShelfId,
        CopyStatus actualStatus, CopyCondition actualCondition, DateTime now)
    {
        var item = CreateExpected(auditId, copy);
        item.IsExpected = false;
        item.Scan(actualShelfId, actualStatus, actualCondition, now);
        return item;
    }

    public void Scan(Guid actualShelfId, CopyStatus actualStatus, CopyCondition actualCondition, DateTime now)
    {
        if (Result != AuditItemResult.Pending) throw new InvalidOperationException("Bản sao đã được quét trong đợt này.");
        if (actualShelfId == Guid.Empty || !Enum.IsDefined(actualStatus) || !Enum.IsDefined(actualCondition))
            throw new ArgumentException("Vị trí hoặc trạng thái quét không hợp lệ.");
        ActualShelfId = actualShelfId;
        ActualStatus = actualStatus;
        ActualCondition = actualCondition;
        Result = !IsExpected ? AuditItemResult.Unexpected
            : actualShelfId != ExpectedShelfId ? AuditItemResult.Misplaced
            : actualStatus != ExpectedStatus ? AuditItemResult.StatusMismatch
            : actualCondition == CopyCondition.Damaged ? AuditItemResult.Damaged
            : actualCondition != ExpectedCondition ? AuditItemResult.ConditionMismatch
            : AuditItemResult.Found;
        ScannedAtUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkMissing()
    {
        if (Result != AuditItemResult.Pending || !IsExpected) throw new InvalidOperationException("Chỉ bản sao dự kiến chưa quét mới được đánh dấu thiếu.");
        Result = AuditItemResult.Missing;
        ConcurrencyToken = Guid.NewGuid();
    }
}
