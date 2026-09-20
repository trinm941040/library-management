using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class InventoryAudit
{
    private InventoryAudit() { }

    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? AreaId { get; private set; }
    public Guid? ShelfId { get; private set; }
    public Guid StartedByUserId { get; private set; }
    public InventoryAuditStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static InventoryAudit Create(Guid branchId, Guid? areaId, Guid? shelfId,
        Guid actorId, string? notes, DateTime now)
    {
        if (branchId == Guid.Empty || actorId == Guid.Empty || areaId == Guid.Empty || shelfId == Guid.Empty)
            throw new ArgumentException("Chi nhánh, người lập và phạm vi phải hợp lệ.");
        if (shelfId is not null && areaId is null)
            throw new ArgumentException("Kệ phải thuộc khu vực đã chọn.");
        if (notes?.Trim().Length > 2000) throw new ArgumentException("Ghi chú tối đa 2000 ký tự.");
        return new InventoryAudit { Id = Guid.NewGuid(), BranchId = branchId,
            AreaId = areaId, ShelfId = shelfId, StartedByUserId = actorId,
            Status = InventoryAuditStatus.InProgress,
            StartedAtUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), ConcurrencyToken = Guid.NewGuid() };
    }

    public void Touch()
    {
        if (Status != InventoryAuditStatus.InProgress) throw new InvalidOperationException("Đợt kiểm kê đã khóa.");
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Complete(DateTime now)
    {
        Touch();
        Status = InventoryAuditStatus.Completed;
        CompletedAtUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
    }
}
