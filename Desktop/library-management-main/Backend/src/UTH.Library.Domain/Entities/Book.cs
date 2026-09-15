using UTH.Library.Domain.ValueObjects;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Book
{
    private Book(
        Guid id,
        string title,
        string author,
        string isbn,
        string category,
        int quantity,
        DateTime createdAtUtc)
    {
        Id = id;
        Title = title;
        Author = author;
        Isbn = isbn;
        Category = category;
        Quantity = quantity;
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

    public int Quantity { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public Guid? PublisherId { get; private set; }
    public RecordStatus Status { get; private set; } = RecordStatus.Active;
    public string? EditionStatement { get; private set; }
    public string? Description { get; private set; }
    public int? PublicationYear { get; private set; }

    public static Book Create(
        string title,
        string author,
        string isbn,
        string category,
        int quantity,
        DateTime createdAtUtc)
    {
        Validate(title, author, isbn, category, quantity);
        return new Book(
            Guid.NewGuid(),
            title.Trim(),
            author.Trim(),
            NormalizeIsbn(isbn),
            category.Trim(),
            quantity,
            createdAtUtc);
    }

    public void SetPublicationMetadata(
        Guid? publisherId,
        string? editionStatement,
        string? description,
        int? publicationYear)
    {
        if (publisherId == Guid.Empty)
            throw new ArgumentException("Publisher is invalid.", nameof(publisherId));
        if (publicationYear is < 0 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(publicationYear), "Publication year is invalid.");

        PublisherId = publisherId;
        EditionStatement = string.IsNullOrWhiteSpace(editionStatement) ? null : editionStatement.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PublicationYear = publicationYear;
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
        int quantity,
        DateTime updatedAtUtc)
    {
        Validate(title, author, isbn, category, quantity);
        Title = title.Trim();
        Author = author.Trim();
        Isbn = NormalizeIsbn(isbn);
        Category = category.Trim();
        Quantity = quantity;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Checkout(DateTime updatedAtUtc)
    {
        if (Quantity <= 0)
            throw new InvalidOperationException("Book is out of stock.");

        Quantity--;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void CheckIn(DateTime updatedAtUtc)
    {
        Quantity++;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    private static void Validate(string title, string author, string isbn, string category, int quantity)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Book author is required.", nameof(author));
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("Book ISBN is required.", nameof(isbn));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Book category is required.", nameof(category));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Book quantity cannot be negative.");
    }

    private static string NormalizeIsbn(string isbn) => IsbnValue.Create(isbn).Value;
}
