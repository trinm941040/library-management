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
            throw new ArgumentException("Độc giả là bắt buộc.", nameof(borrowerId));
        if (string.IsNullOrWhiteSpace(borrowerName))
            throw new ArgumentException("Tên độc giả là bắt buộc.", nameof(borrowerName));
        if (string.IsNullOrWhiteSpace(borrowerEmail))
            throw new ArgumentException("Email độc giả là bắt buộc.", nameof(borrowerEmail));
        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type.Trim().ToLowerInvariant()))
            throw new ArgumentException("Loại vi phạm không hợp lệ.", nameof(type));
        if (fineAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fineAmount), "Tiền phạt không được là số âm.");

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
            throw new InvalidOperationException("Vi phạm đã được xử lý.");

        ResolvedAtUtc = resolvedAtUtc;
        Resolution = resolution;
        ConcurrencyToken = Guid.NewGuid();
    }

    public Guid? ExtractBorrowingId()
    {
        if (string.IsNullOrWhiteSpace(AppliedPolicySnapshot)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(AppliedPolicySnapshot);
            if (doc.RootElement.TryGetProperty("BorrowingId", out var prop) && prop.TryGetGuid(out var g))
                return g;
        }
        catch { }
        return null;
    }

    public Guid? ExtractBookCopyId()
    {
        if (string.IsNullOrWhiteSpace(AppliedPolicySnapshot)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(AppliedPolicySnapshot);
            if (doc.RootElement.TryGetProperty("BookCopyId", out var prop) && prop.TryGetGuid(out var g))
                return g;
        }
        catch { }
        return null;
    }

    public string? ExtractBookCopyBarcode()
    {
        if (string.IsNullOrWhiteSpace(AppliedPolicySnapshot)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(AppliedPolicySnapshot);
            if (doc.RootElement.TryGetProperty("BookCopyBarcode", out var prop))
                return prop.GetString();
        }
        catch { }
        return null;
    }

    public string? ExtractCalculationBasis()
    {
        if (string.IsNullOrWhiteSpace(AppliedPolicySnapshot)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(AppliedPolicySnapshot);
            if (doc.RootElement.TryGetProperty("CalculationBasis", out var prop))
                return prop.GetRawText();
        }
        catch { }
        return null;
    }
}
