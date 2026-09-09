namespace UTH.Library.Domain.Entities;

public sealed class MembershipCard
{
    private MembershipCard() { CardNumber = string.Empty; }
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public string CardNumber { get; private set; }
    public DateOnly IssuedOn { get; private set; }
    public DateOnly ExpiresOn { get; private set; }
    public MembershipCardStatus Status { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static MembershipCard Issue(Guid memberId, string cardNumber, DateOnly issuedOn, DateOnly expiresOn, DateTime now)
    {
        if (memberId == Guid.Empty) throw new ArgumentException("Member is required.");
        if (string.IsNullOrWhiteSpace(cardNumber)) throw new ArgumentException("Card number is required.");
        if (expiresOn <= issuedOn) throw new ArgumentException("Card expiration must be after issue date.");
        return new MembershipCard { Id = Guid.NewGuid(), MemberId = memberId, CardNumber = cardNumber.Trim().ToUpperInvariant(), IssuedOn = issuedOn, ExpiresOn = expiresOn, Status = MembershipCardStatus.Active, UpdatedAtUtc = now };
    }

    public void Renew(DateOnly expiresOn, DateTime now)
    {
        if (expiresOn <= ExpiresOn) throw new ArgumentException("New expiration must be later than the current expiration.");
        ExpiresOn = expiresOn;
        Status = MembershipCardStatus.Active;
        UpdatedAtUtc = now;
    }

    public void ChangeStatus(MembershipCardStatus status, DateTime now)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException("Card status is invalid.");
        Status = status;
        UpdatedAtUtc = now;
    }
}
