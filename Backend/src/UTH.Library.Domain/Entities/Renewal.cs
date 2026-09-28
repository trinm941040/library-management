namespace UTH.Library.Domain.Entities;

public sealed class Renewal
{
    private Renewal() { AppliedPolicySnapshot = string.Empty; }

    public Guid Id { get; private set; }
    public Guid BorrowingId { get; private set; }
    public DateTime PreviousDueAtUtc { get; private set; }
    public DateTime NewDueAtUtc { get; private set; }
    public Guid RenewedByUserId { get; private set; }
    public DateTime RenewedAtUtc { get; private set; }
    public Guid? AppliedPolicyId { get; private set; }
    public int AppliedPolicyVersion { get; private set; }
    public string AppliedPolicySnapshot { get; private set; }

    public static Renewal Create(
        Guid borrowingId,
        DateTime previousDueAtUtc,
        DateTime newDueAtUtc,
        Guid renewedByUserId,
        DateTime renewedAtUtc,
        Guid? appliedPolicyId,
        int appliedPolicyVersion,
        string appliedPolicySnapshot)
    {
        if (borrowingId == Guid.Empty || renewedByUserId == Guid.Empty)
            throw new ArgumentException("Khoản mượn và người thực hiện gia hạn là bắt buộc.");
        if (newDueAtUtc <= previousDueAtUtc)
            throw new ArgumentException("Hạn trả mới phải sau hạn trả trước đó.");

        return new Renewal
        {
            Id = Guid.NewGuid(),
            BorrowingId = borrowingId,
            PreviousDueAtUtc = previousDueAtUtc,
            NewDueAtUtc = newDueAtUtc,
            RenewedByUserId = renewedByUserId,
            RenewedAtUtc = renewedAtUtc,
            AppliedPolicyId = appliedPolicyId,
            AppliedPolicyVersion = appliedPolicyVersion,
            AppliedPolicySnapshot = string.IsNullOrWhiteSpace(appliedPolicySnapshot) ? "{}" : appliedPolicySnapshot
        };
    }
}
