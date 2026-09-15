using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class ReservationTests
{
    [Fact]
    public void Create_EmptyBook_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Reservation.Create(Guid.Empty, Guid.NewGuid(), "Olivia", "olivia@example.com", DateTime.UtcNow, 7));
    }

    [Fact]
    public void MarkCancelled_OpenReservation_SetsCancelledAt()
    {
        var now = DateTime.UtcNow;
        var reservation = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), "Olivia", "olivia@example.com", now, 7);

        reservation.MarkCancelled(now.AddHours(1));

        Assert.True(reservation.IsCancelled);
        Assert.False(reservation.IsOpen);
    }

    [Fact]
    public void MarkFulfilled_ExpiredReservation_StillMarksFulfilled()
    {
        var now = DateTime.UtcNow;
        var reservation = Reservation.Create(Guid.NewGuid(), Guid.NewGuid(), "Olivia", "olivia@example.com", now.AddDays(-10), 7);

        Assert.True(reservation.IsExpired(now));
        reservation.MarkFulfilled(now);
        Assert.True(reservation.IsFulfilled);
    }
}
