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
        if (memberId == Guid.Empty) throw new ArgumentException("Độc giả là bắt buộc.");
        if (string.IsNullOrWhiteSpace(cardNumber)) throw new ArgumentException("Số thẻ là bắt buộc.");
        if (issuedOn > DateOnly.FromDateTime(now)) throw new ArgumentException("Ngày cấp thẻ không được ở tương lai.");
        if (expiresOn <= issuedOn) throw new ArgumentException("Ngày hết hạn phải sau ngày cấp thẻ.");
        return new MembershipCard { Id = Guid.NewGuid(), MemberId = memberId, CardNumber = cardNumber.Trim().ToUpperInvariant(), IssuedOn = issuedOn, ExpiresOn = expiresOn, Status = MembershipCardStatus.Active, UpdatedAtUtc = now };
    }

    public void Renew(DateOnly expiresOn, DateTime now)
    {
        if (Status == MembershipCardStatus.Revoked) throw new InvalidOperationException("Thẻ đã thu hồi không thể gia hạn.");
        if (expiresOn <= ExpiresOn) throw new ArgumentException("Ngày hết hạn mới phải sau ngày hết hạn hiện tại.");
        ExpiresOn = expiresOn;
        Status = MembershipCardStatus.Active;
        UpdatedAtUtc = now;
    }

    public void ChangeStatus(MembershipCardStatus status, DateTime now)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException("Trạng thái thẻ không hợp lệ.");
        if (Status == MembershipCardStatus.Revoked && status != MembershipCardStatus.Revoked)
            throw new InvalidOperationException("Thẻ đã thu hồi không thể kích hoạt lại.");
        Status = status;
        UpdatedAtUtc = now;
    }
}
