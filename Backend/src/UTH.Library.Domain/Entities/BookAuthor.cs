namespace UTH.Library.Domain.Entities;

public sealed class BookAuthor
{
    private BookAuthor(Guid bookId, Guid authorId)
    {
        BookId = bookId;
        AuthorId = authorId;
    }

    private BookAuthor() { }

    public Guid BookId { get; private set; }
    public Guid AuthorId { get; private set; }

    public static BookAuthor Create(Guid bookId, Guid authorId) => new(bookId, authorId);
}
