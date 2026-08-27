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
    }

    public void Checkout(DateTime updatedAtUtc)
    {
        if (Quantity <= 0)
            throw new InvalidOperationException("Book is out of stock.");

        Quantity--;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void CheckIn(DateTime updatedAtUtc)
    {
        Quantity++;
        UpdatedAtUtc = updatedAtUtc;
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

    private static string NormalizeIsbn(string isbn) =>
        isbn.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
}
