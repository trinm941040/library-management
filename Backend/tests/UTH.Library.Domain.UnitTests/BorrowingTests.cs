using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class BorrowingTests
{
    [Fact]
    public void Create_EmptyBook_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Borrowing.Create(Guid.Empty, Guid.NewGuid(), "Olivia", "olivia@example.com", DateTime.UtcNow, 14));
    }

    [Fact]
    public void MarkReturned_ActiveBorrowing_SetsReturnedAt()
    {
        var now = DateTime.UtcNow;
        var borrowing = Borrowing.Create(Guid.NewGuid(), Guid.NewGuid(), "Olivia", "olivia@example.com", now, 14);

        borrowing.MarkReturned(now.AddDays(3));

        Assert.True(borrowing.IsReturned);
        Assert.False(borrowing.IsOverdue(now.AddDays(20)));
    }

    [Fact]
    public void IsOverdue_PastDueAndNotReturned_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var borrowing = Borrowing.Create(Guid.NewGuid(), Guid.NewGuid(), "Olivia", "olivia@example.com", now.AddDays(-20), 14);

        Assert.True(borrowing.IsOverdue(now));
    }
}
