using UTH.Library.Domain.Entities;
namespace UTH.Library.Domain.UnitTests;

public sealed class MemberTests
{
    [Fact]
    public void Create_NormalizesIdentityAndPolicyValues()
    {
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        var member = Member.Create(" rd-001 ", " Nguyen Van A ", " READER@EXAMPLE.COM ", " 0901 ",
            new DateOnly(2000, 1, 1), " HCMC ", " Sinh viên ", MemberStatus.Active, 5, 14, now);
        Assert.Equal("RD-001", member.MemberCode);
        Assert.Equal("reader@example.com", member.Email);
        Assert.Equal(5, member.BorrowingLimit);
        Assert.NotEqual(Guid.Empty, member.ConcurrencyToken);
    }

    [Fact]
    public void IssueCard_ExpirationNotAfterIssue_Throws()
    {
        var day = new DateOnly(2026, 9, 8);
        Assert.Throws<ArgumentException>(() => MembershipCard.Issue(Guid.NewGuid(), "CARD-1", day, day, DateTime.UtcNow));
    }

    [Fact]
    public void Restriction_Remove_PreservesReasonAndActor()
    {
        var actor = Guid.NewGuid(); var now = DateTime.UtcNow;
        var restriction = MemberRestriction.Create(Guid.NewGuid(), MemberRestrictionType.Borrowing, "Nợ sách", now, now.AddDays(7), actor);
        restriction.Remove("Đã hoàn tất nghĩa vụ", now.AddDays(1), actor);
        Assert.Equal("Đã hoàn tất nghĩa vụ", restriction.RemovalReason);
        Assert.Equal(actor, restriction.RemovedByUserId);
    }

    [Fact]
    public void FinePayment_ZeroAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FinePayment.Create(Guid.NewGuid(), Guid.NewGuid(), 0, FinePaymentMethod.Cash, null, DateTime.UtcNow, null));
    }
}
