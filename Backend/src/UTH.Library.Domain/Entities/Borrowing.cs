namespace UTH.Library.Domain.Entities;

public sealed class Borrowing
{
    public const int DefaultLoanDays = 14;

    private Borrowing(
        Guid id,
        Guid bookId,
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        DateTime borrowedAtUtc,
        DateTime dueAtUtc,
        Guid? appliedPolicyId,
        int appliedPolicyVersion,
        string appliedPolicySnapshot)
    {
        Id = id;
        BookId = bookId;
        BorrowerId = borrowerId;
        BorrowerName = borrowerName;
        BorrowerEmail = borrowerEmail;
        BorrowedAtUtc = borrowedAtUtc;
        DueAtUtc = dueAtUtc;
        AppliedPolicyId = appliedPolicyId;
        AppliedPolicyVersion = appliedPolicyVersion;
        AppliedPolicySnapshot = appliedPolicySnapshot;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Borrowing()
    {
        BorrowerName = string.Empty;
        BorrowerEmail = string.Empty;
        AppliedPolicySnapshot = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerName { get; private set; }
    public string BorrowerEmail { get; private set; }
    public DateTime BorrowedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime? ReturnedAtUtc { get; private set; }
    public int RenewalCount { get; private set; }
    public Guid? AppliedPolicyId { get; private set; }
    public int AppliedPolicyVersion { get; private set; }
    public string AppliedPolicySnapshot { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public bool IsReturned => ReturnedAtUtc is not null;

    public static Borrowing Create(
        Guid bookId,
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        DateTime borrowedAtUtc,
        int loanDays,
        Guid? appliedPolicyId = null,
        int appliedPolicyVersion = 1,
        string appliedPolicySnapshot = "{}")
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Book is required.", nameof(bookId));
        if (borrowerId == Guid.Empty)
            throw new ArgumentException("Borrower is required.", nameof(borrowerId));
        if (string.IsNullOrWhiteSpace(borrowerName))
            throw new ArgumentException("Borrower name is required.", nameof(borrowerName));
        if (string.IsNullOrWhiteSpace(borrowerEmail))
            throw new ArgumentException("Borrower email is required.", nameof(borrowerEmail));
        if (loanDays < 1)
            throw new ArgumentOutOfRangeException(nameof(loanDays), "Loan period must be at least 1 day.");
        if (appliedPolicyVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(appliedPolicyVersion));

        return new Borrowing(
            Guid.NewGuid(),
            bookId,
            borrowerId,
            borrowerName.Trim(),
            borrowerEmail.Trim(),
            borrowedAtUtc,
            borrowedAtUtc.AddDays(loanDays),
            appliedPolicyId,
            appliedPolicyVersion,
            string.IsNullOrWhiteSpace(appliedPolicySnapshot) ? "{}" : appliedPolicySnapshot);
    }

    public void MarkReturned(DateTime returnedAtUtc)
    {
        if (IsReturned)
            throw new InvalidOperationException("Borrowing is already returned.");

        ReturnedAtUtc = returnedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public bool IsOverdue(DateTime utcNow) => !IsReturned && DueAtUtc < utcNow;

    public void Renew(int renewalDays, int maxRenewals)
    {
        if (IsReturned)
            throw new InvalidOperationException("Borrowing is already returned.");
        if (renewalDays < 1)
            throw new ArgumentOutOfRangeException(nameof(renewalDays));
        if (RenewalCount >= maxRenewals)
            throw new InvalidOperationException("Maximum number of renewals has been reached.");

        DueAtUtc = DueAtUtc.AddDays(renewalDays);
        RenewalCount++;
        ConcurrencyToken = Guid.NewGuid();
    }
}
