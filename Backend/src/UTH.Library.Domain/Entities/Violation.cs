namespace UTH.Library.Domain.Entities;

public sealed class Violation
{
    public static readonly IReadOnlyList<string> AllowedTypes = ["overdue", "damage", "lost", "other"];

    private Violation(
        Guid id,
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        Guid? bookId,
        string bookTitle,
        string type,
        string note,
        decimal fineAmount,
        DateTime recordedAtUtc,
        Guid? appliedPolicyId,
        int appliedPolicyVersion,
        string appliedPolicySnapshot)
    {
        Id = id;
        BorrowerId = borrowerId;
        BorrowerName = borrowerName;
        BorrowerEmail = borrowerEmail;
        BookId = bookId;
        BookTitle = bookTitle;
        Type = type;
        Note = note;
        FineAmount = fineAmount;
        RecordedAtUtc = recordedAtUtc;
        AppliedPolicyId = appliedPolicyId;
        AppliedPolicyVersion = appliedPolicyVersion;
        AppliedPolicySnapshot = appliedPolicySnapshot;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Violation()
    {
        BorrowerName = string.Empty;
        BorrowerEmail = string.Empty;
        BookTitle = string.Empty;
        Type = "other";
        Note = string.Empty;
        AppliedPolicySnapshot = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerName { get; private set; }
    public string BorrowerEmail { get; private set; }
    public Guid? BookId { get; private set; }
    public string BookTitle { get; private set; }
    public string Type { get; private set; }
    public string Note { get; private set; }
    public decimal FineAmount { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? Resolution { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public Guid? AppliedPolicyId { get; private set; }
    public int AppliedPolicyVersion { get; private set; }
    public string AppliedPolicySnapshot { get; private set; }

    public bool IsOpen => ResolvedAtUtc is null;

    public static Violation Create(
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        Guid? bookId,
        string bookTitle,
        string type,
        string note,
        decimal fineAmount,
        DateTime recordedAtUtc,
        Guid? appliedPolicyId = null,
        int appliedPolicyVersion = 1,
        string appliedPolicySnapshot = "{}")
    {
        if (borrowerId == Guid.Empty)
            throw new ArgumentException("Borrower is required.", nameof(borrowerId));
        if (string.IsNullOrWhiteSpace(borrowerName))
            throw new ArgumentException("Borrower name is required.", nameof(borrowerName));
        if (string.IsNullOrWhiteSpace(borrowerEmail))
            throw new ArgumentException("Borrower email is required.", nameof(borrowerEmail));
        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type.Trim().ToLowerInvariant()))
            throw new ArgumentException("Violation type is invalid.", nameof(type));
        if (fineAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fineAmount), "Fine amount cannot be negative.");

        return new Violation(
            Guid.NewGuid(),
            borrowerId,
            borrowerName.Trim(),
            borrowerEmail.Trim(),
            bookId is Guid id && id != Guid.Empty ? id : null,
            bookTitle.Trim(),
            type.Trim().ToLowerInvariant(),
            note.Trim(),
            fineAmount,
            recordedAtUtc,
            appliedPolicyId,
            appliedPolicyVersion,
            string.IsNullOrWhiteSpace(appliedPolicySnapshot) ? "{}" : appliedPolicySnapshot);
    }

    public void MarkPaid(DateTime resolvedAtUtc) => Resolve(resolvedAtUtc, "paid");

    public void MarkWaived(DateTime resolvedAtUtc) => Resolve(resolvedAtUtc, "waived");

    private void Resolve(DateTime resolvedAtUtc, string resolution)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Violation is already resolved.");

        ResolvedAtUtc = resolvedAtUtc;
        Resolution = resolution;
        ConcurrencyToken = Guid.NewGuid();
    }
}
