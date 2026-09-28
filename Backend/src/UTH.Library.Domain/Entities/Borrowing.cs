namespace UTH.Library.Domain.Entities;

public sealed class Borrowing
{
    public const int DefaultLoanDays = 14;

    private Borrowing(
        Guid id,
        Guid bookId,
        Guid? bookCopyId,
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        DateTime borrowedAtUtc,
        DateTime dueAtUtc,
        Guid? processedByEmployeeId,
        Guid? appliedPolicyId,
        int appliedPolicyVersion,
        string appliedPolicySnapshot)
    {
        Id = id;
        BookId = bookId;
        BookCopyId = bookCopyId;
        BorrowerId = borrowerId;
        BorrowerName = borrowerName;
        BorrowerEmail = borrowerEmail;
        BorrowedAtUtc = borrowedAtUtc;
        DueAtUtc = dueAtUtc;
        ProcessedByEmployeeId = processedByEmployeeId;
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
    public Guid? BookCopyId { get; private set; }
    public Guid BorrowerId { get; private set; }
    public string BorrowerName { get; private set; }
    public string BorrowerEmail { get; private set; }
    public DateTime BorrowedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime? ReturnedAtUtc { get; private set; }
    public int RenewalCount { get; private set; }
    public Guid? ProcessedByEmployeeId { get; private set; }
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
        string appliedPolicySnapshot = "{}") =>
        CreateWithCopy(
            bookId,
            null,
            borrowerId,
            borrowerName,
            borrowerEmail,
            borrowedAtUtc,
            loanDays,
            null,
            appliedPolicyId,
            appliedPolicyVersion,
            appliedPolicySnapshot);

    public static Borrowing CreateWithCopy(
        Guid bookId,
        Guid? bookCopyId,
        Guid borrowerId,
        string borrowerName,
        string borrowerEmail,
        DateTime borrowedAtUtc,
        int loanDays,
        Guid? processedByEmployeeId = null,
        Guid? appliedPolicyId = null,
        int appliedPolicyVersion = 1,
        string appliedPolicySnapshot = "{}")
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Sách là bắt buộc.", nameof(bookId));
        if (borrowerId == Guid.Empty)
            throw new ArgumentException("Độc giả là bắt buộc.", nameof(borrowerId));
        if (string.IsNullOrWhiteSpace(borrowerName))
            throw new ArgumentException("Tên độc giả là bắt buộc.", nameof(borrowerName));
        if (string.IsNullOrWhiteSpace(borrowerEmail))
            throw new ArgumentException("Email độc giả là bắt buộc.", nameof(borrowerEmail));
        if (loanDays < 1)
            throw new ArgumentOutOfRangeException(nameof(loanDays), "Thời hạn mượn phải ít nhất 1 ngày.");
        if (appliedPolicyVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(appliedPolicyVersion));

        return new Borrowing(
            Guid.NewGuid(),
            bookId,
            bookCopyId,
            borrowerId,
            borrowerName.Trim(),
            borrowerEmail.Trim(),
            borrowedAtUtc,
            borrowedAtUtc.AddDays(loanDays),
            processedByEmployeeId,
            appliedPolicyId,
            appliedPolicyVersion,
            string.IsNullOrWhiteSpace(appliedPolicySnapshot) ? "{}" : appliedPolicySnapshot);
    }

    public void MarkReturned(DateTime returnedAtUtc)
    {
        if (IsReturned)
            throw new InvalidOperationException("Khoản mượn đã được trả.");

        ReturnedAtUtc = returnedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public bool IsOverdue(DateTime utcNow) => !IsReturned && DueAtUtc < utcNow;

    public void Renew(int renewalDays, int maxRenewals)
    {
        if (IsReturned)
            throw new InvalidOperationException("Khoản mượn đã được trả.");
        if (renewalDays < 1)
            throw new ArgumentOutOfRangeException(nameof(renewalDays));
        if (RenewalCount >= maxRenewals)
            throw new InvalidOperationException("Khoản mượn đã đạt số lần gia hạn tối đa.");

        DueAtUtc = DueAtUtc.AddDays(renewalDays);
        RenewalCount++;
        ConcurrencyToken = Guid.NewGuid();
    }
}
