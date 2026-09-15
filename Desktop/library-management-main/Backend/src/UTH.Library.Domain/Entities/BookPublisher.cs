namespace UTH.Library.Domain.Entities;

public sealed class BookPublisher
{
    private BookPublisher(Guid bookId, Guid publisherId) { BookId = bookId; PublisherId = publisherId; }
    private BookPublisher() { }
    public Guid BookId { get; private set; }
    public Guid PublisherId { get; private set; }
    public static BookPublisher Create(Guid bookId, Guid publisherId) => new(bookId, publisherId);
}