namespace UTH.Library.Domain.Entities;

public sealed class Reservation
{
    public const int DefaultHoldDays = 7;

    private Reservation(
        Guid id,
        Guid bookId,
        Guid reserverId,
        string reserverName,
        string reserverEmail,
        DateTime reservedAtUtc,
        DateTime expiresAtUtc,
        Guid? appliedPolicyId,
        int appliedPolicyVersion,
        string appliedPolicySnapshot)
    {
        Id = id;
        BookId = bookId;
        ReserverId = reserverId;
        ReserverName = reserverName;
        ReserverEmail = reserverEmail;
        ReservedAtUtc = reservedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AppliedPolicyId = appliedPolicyId;
        AppliedPolicyVersion = appliedPolicyVersion;
        AppliedPolicySnapshot = appliedPolicySnapshot;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Reservation()
    {
        ReserverName = string.Empty;
        ReserverEmail = string.Empty;
        AppliedPolicySnapshot = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid ReserverId { get; private set; }
    public string ReserverName { get; private set; }
    public string ReserverEmail { get; private set; }
    public DateTime ReservedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? FulfilledAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? AppliedPolicyId { get; private set; }
    public int AppliedPolicyVersion { get; private set; }
    public string AppliedPolicySnapshot { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public bool IsFulfilled => FulfilledAtUtc is not null;
    public bool IsCancelled => CancelledAtUtc is not null;
    public bool IsOpen => !IsFulfilled && !IsCancelled;

    public static Reservation Create(
        Guid bookId,
        Guid reserverId,
        string reserverName,
        string reserverEmail,
        DateTime reservedAtUtc,
        int holdDays,
        Guid? appliedPolicyId = null,
        int appliedPolicyVersion = 1,
        string appliedPolicySnapshot = "{}")
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Book is required.", nameof(bookId));
        if (reserverId == Guid.Empty)
            throw new ArgumentException("Reserver is required.", nameof(reserverId));
        if (string.IsNullOrWhiteSpace(reserverName))
            throw new ArgumentException("Reserver name is required.", nameof(reserverName));
        if (string.IsNullOrWhiteSpace(reserverEmail))
            throw new ArgumentException("Reserver email is required.", nameof(reserverEmail));
        if (holdDays < 1)
            throw new ArgumentOutOfRangeException(nameof(holdDays), "Hold period must be at least 1 day.");

        return new Reservation(
            Guid.NewGuid(),
            bookId,
            reserverId,
            reserverName.Trim(),
            reserverEmail.Trim(),
            reservedAtUtc,
            reservedAtUtc.AddDays(holdDays),
            appliedPolicyId,
            appliedPolicyVersion,
            string.IsNullOrWhiteSpace(appliedPolicySnapshot) ? "{}" : appliedPolicySnapshot);
    }

    public void MarkCancelled(DateTime cancelledAtUtc)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Reservation is no longer active.");

        CancelledAtUtc = cancelledAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkFulfilled(DateTime fulfilledAtUtc)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Reservation is no longer active.");

        FulfilledAtUtc = fulfilledAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public bool IsExpired(DateTime utcNow) => IsOpen && ExpiresAtUtc < utcNow;
}
