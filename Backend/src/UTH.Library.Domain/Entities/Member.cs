using System.Text.Json.Serialization;
using System.Net.Mail;
using System.ComponentModel.DataAnnotations.Schema;
namespace UTH.Library.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter<MemberStatus>))]
public enum MemberStatus { Active, Expired, Suspended, Discontinued }
[JsonConverter(typeof(JsonStringEnumConverter<MembershipCardStatus>))]
public enum MembershipCardStatus { Active, Expired, Suspended, Revoked }
[JsonConverter(typeof(JsonStringEnumConverter<MemberRestrictionType>))]
public enum MemberRestrictionType { Borrowing, Reservation, AllTransactions }
[JsonConverter(typeof(JsonStringEnumConverter<FinePaymentMethod>))]
public enum FinePaymentMethod { Cash, BankTransfer, Card, Other }

public sealed class Member
{
    private Member() { MemberCode = FullName = Email = MemberGroup = string.Empty; }

    public Guid Id { get; private set; }
    public string MemberCode { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string MemberGroup { get; private set; }
    public MemberStatus Status { get; private set; }
    public int BorrowingLimit { get; private set; }
    public int LoanPeriodDays { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public ICollection<MembershipCard> MembershipCards { get; private set; } = new List<MembershipCard>();
    [NotMapped]
    public MembershipCard? MembershipCard => MembershipCards
        .Where(card => card.Status != MembershipCardStatus.Revoked)
        .OrderByDescending(card => card.IssuedOn)
        .ThenByDescending(card => card.UpdatedAtUtc)
        .FirstOrDefault()
        ?? MembershipCards
            .OrderByDescending(card => card.IssuedOn)
            .ThenByDescending(card => card.UpdatedAtUtc)
            .FirstOrDefault();
    public ICollection<MemberRestriction> Restrictions { get; private set; } = new List<MemberRestriction>();

    public static Member Create(string memberCode, string fullName, string email, string? phoneNumber,
        DateOnly? dateOfBirth, string? address, string memberGroup, MemberStatus status,
        int borrowingLimit, int loanPeriodDays, DateTime now)
    {
        var member = new Member { Id = Guid.NewGuid(), CreatedAtUtc = now };
        member.Update(memberCode, fullName, email, phoneNumber, dateOfBirth, address, memberGroup,
            status, borrowingLimit, loanPeriodDays, now);
        return member;
    }

    public void Update(string memberCode, string fullName, string email, string? phoneNumber,
        DateOnly? dateOfBirth, string? address, string memberGroup, MemberStatus status,
        int borrowingLimit, int loanPeriodDays, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(memberCode)) throw new ArgumentException("Member code is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(memberGroup)) throw new ArgumentException("Member group is required.");
        if (!Enum.IsDefined(status)) throw new ArgumentException("Member status is invalid.");
        if (borrowingLimit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(borrowingLimit));
        if (loanPeriodDays is < 1 or > 365) throw new ArgumentOutOfRangeException(nameof(loanPeriodDays));
        if (dateOfBirth is not null && dateOfBirth >= DateOnly.FromDateTime(now))
            throw new ArgumentException("Date of birth must be in the past.");
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail) || parsedEmail.Address != normalizedEmail)
            throw new ArgumentException("Email address is invalid.");
        MemberCode = memberCode.Trim().ToUpperInvariant();
        FullName = fullName.Trim();
        Email = normalizedEmail;
        PhoneNumber = NullIfBlank(phoneNumber);
        DateOfBirth = dateOfBirth;
        Address = NullIfBlank(address);
        MemberGroup = memberGroup.Trim();
        Status = status;
        BorrowingLimit = borrowingLimit;
        LoanPeriodDays = loanPeriodDays;
        UpdatedAtUtc = now;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Touch(DateTime now)
    {
        UpdatedAtUtc = now;
        ConcurrencyToken = Guid.NewGuid();
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
