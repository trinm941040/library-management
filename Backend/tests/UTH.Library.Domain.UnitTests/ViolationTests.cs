using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class ViolationTests
{
    [Fact]
    public void Create_EmptyBorrower_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Violation.Create(Guid.Empty, "Olivia", "olivia@example.com", null, "", "overdue", "", 10_000, DateTime.UtcNow));
    }

    [Fact]
    public void Create_InvalidType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Violation.Create(Guid.NewGuid(), "Olivia", "olivia@example.com", null, "", "latefee", "", 10_000, DateTime.UtcNow));
    }

    [Fact]
    public void MarkPaid_OpenViolation_SetsResolution()
    {
        var now = DateTime.UtcNow;
        var violation = Violation.Create(Guid.NewGuid(), "Olivia", "olivia@example.com", null, "", "overdue", "Trễ 3 ngày", 15_000, now);

        violation.MarkPaid(now.AddDays(1));

        Assert.False(violation.IsOpen);
        Assert.Equal("paid", violation.Resolution);
    }
}
