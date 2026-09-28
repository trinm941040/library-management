using UTH.Library.Domain.Enums;
using UTH.Library.Domain.ValueObjects;

namespace UTH.Library.Domain.Entities;

public sealed class Book
{
    private Book(
        Guid id,
        string title,
        string author,
        string isbn,
        string category,
        DateTime createdAtUtc)
    {
        Id = id;
        Title = title;
        Author = author;
        Isbn = isbn;
        Category = category;
        CreatedAtUtc = createdAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Book()
    {
        Title = string.Empty;
        Author = string.Empty;
        Isbn = string.Empty;
        Category = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string Author { get; private set; }

    public string Isbn { get; private set; }

    public string Category { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public Guid? PublisherId { get; private set; }
    public RecordStatus Status { get; private set; } = RecordStatus.Active;
    public string? EditionStatement { get; private set; }
    public string? Description { get; private set; }
    public int? PublicationYear { get; private set; }
    public string? Language { get; private set; }
    public int? PageCount { get; private set; }

    public static Book Create(
        string title,
        string author,
        string isbn,
        string category,
        DateTime createdAtUtc)
    {
        Validate(title, author, isbn, category);
        return new Book(
            Guid.NewGuid(),
            title.Trim(),
            author.Trim(),
            NormalizeIsbn(isbn),
            category.Trim(),
            createdAtUtc);
    }

    public void SetPublicationMetadata(
        Guid? publisherId,
        string? editionStatement,
        string? description,
        int? publicationYear,
        string? language = null,
        int? pageCount = null)
    {
        if (publisherId == Guid.Empty)
            throw new ArgumentException("Nhà xuất bản không hợp lệ.", nameof(publisherId));
        if (publicationYear is < 0 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(publicationYear), "Năm xuất bản không hợp lệ.");
        if (pageCount is <= 0 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Số trang không hợp lệ.");

        PublisherId = publisherId;
        EditionStatement = string.IsNullOrWhiteSpace(editionStatement) ? null : editionStatement.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PublicationYear = publicationYear;
        Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
        PageCount = pageCount;
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        Status = RecordStatus.Inactive;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Update(
        string title,
        string author,
        string isbn,
        string category,
        DateTime updatedAtUtc)
    {
        Validate(title, author, isbn, category);
        Title = title.Trim();
        Author = author.Trim();
        Isbn = NormalizeIsbn(isbn);
        Category = category.Trim();
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    private static void Validate(string title, string author, string isbn, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tên sách là bắt buộc.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Tác giả là bắt buộc.", nameof(author));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN là bắt buộc.", nameof(isbn));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Thể loại là bắt buộc.", nameof(category));
    }

    private static string NormalizeIsbn(string isbn) => IsbnValue.Create(isbn).Value;
}
