namespace UTH.Library.Domain.Entities;
public sealed class BookCategory
{
	private BookCategory(Guid bookId, Guid categoryId) { BookId = bookId; CategoryId = categoryId; }
	private BookCategory() { }
	public Guid BookId { get; private set; }
	public Guid CategoryId { get; private set; }
	public static BookCategory Create(Guid bookId, Guid categoryId) => new(bookId, categoryId);
}
