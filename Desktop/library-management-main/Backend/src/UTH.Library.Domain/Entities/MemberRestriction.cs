namespace UTH.Library.Domain.Entities;

public sealed class MemberRestriction
{
    private MemberRestriction() { Reason = string.Empty; }
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public MemberRestrictionType Type { get; private set; }
    public string Reason { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }
    public string? RemovalReason { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? RemovedByUserId { get; private set; }

    public static MemberRestriction Create(Guid memberId, MemberRestrictionType type, string reason,
        DateTime startsAtUtc, DateTime? endsAtUtc, Guid? actorUserId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Restriction reason is required.");
        if (endsAtUtc <= startsAtUtc) throw new ArgumentException("Restriction end must be after its start.");
        return new MemberRestriction { Id = Guid.NewGuid(), MemberId = memberId, Type = type, Reason = reason.Trim(), StartsAtUtc = startsAtUtc, EndsAtUtc = endsAtUtc, CreatedByUserId = actorUserId };
    }

    public void Remove(string reason, DateTime now, Guid? actorUserId)
    {
        if (RemovedAtUtc is not null) throw new InvalidOperationException("Restriction is already removed.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Removal reason is required.");
        RemovedAtUtc = now; RemovalReason = reason.Trim(); RemovedByUserId = actorUserId;
    }
}
