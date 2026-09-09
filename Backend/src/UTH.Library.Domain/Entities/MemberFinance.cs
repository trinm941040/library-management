namespace UTH.Library.Domain.Entities;

public sealed class FinePayment
{
    private FinePayment() { Reference = string.Empty; }
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid ViolationId { get; private set; }
    public decimal Amount { get; private set; }
    public FinePaymentMethod Method { get; private set; }
    public string Reference { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public static FinePayment Create(Guid memberId, Guid violationId, decimal amount, FinePaymentMethod method, string? reference, DateTime now, Guid? actor) {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return new FinePayment { Id = Guid.NewGuid(), MemberId = memberId, ViolationId = violationId, Amount = amount, Method = method, Reference = reference?.Trim() ?? string.Empty, PaidAtUtc = now, ReceivedByUserId = actor };
    }
}

public sealed class FineAdjustment
{
    private FineAdjustment() { Reason = string.Empty; }
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid ViolationId { get; private set; }
    public decimal AmountDelta { get; private set; }
    public string Reason { get; private set; }
    public DateTime AdjustedAtUtc { get; private set; }
    public Guid? AdjustedByUserId { get; private set; }
    public static FineAdjustment Create(Guid memberId, Guid violationId, decimal amountDelta, string reason, DateTime now, Guid? actor) {
        if (amountDelta == 0) throw new ArgumentOutOfRangeException(nameof(amountDelta));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Adjustment reason is required.");
        return new FineAdjustment { Id = Guid.NewGuid(), MemberId = memberId, ViolationId = violationId, AmountDelta = amountDelta, Reason = reason.Trim(), AdjustedAtUtc = now, AdjustedByUserId = actor };
    }
}
