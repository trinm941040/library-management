namespace UTH.Library.Domain.Entities;
public sealed class Renewal { public Guid Id { get; private set; } public Guid BorrowingId { get; private set; } public DateTime PreviousDueAtUtc { get; private set; } public DateTime NewDueAtUtc { get; private set; } public Guid RenewedByUserId { get; private set; } public DateTime RenewedAtUtc { get; private set; } }
