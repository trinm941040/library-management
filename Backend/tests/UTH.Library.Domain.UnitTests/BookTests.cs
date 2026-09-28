using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class BookTests
{
    [Fact]
    public void Create_EmptyTitle_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Book.Create(" ", "Author", "9780132350884", "Programming", DateTime.UtcNow));
    }

    [Fact]
    public void Create_ValidValues_NormalizesIsbn()
    {
        var book = Book.Create("Clean Code", "Robert C. Martin", "978-0-13-235088-4", "Programming", DateTime.UtcNow);

        Assert.Equal("9780132350884", book.Isbn);
        Assert.Equal("Clean Code", book.Title);
    }

    [Fact]
    public void Update_ValidValues_ChangesFields()
    {
        var book = Book.Create("Clean Code", "Robert C. Martin", "9780132350884", "Programming", DateTime.UtcNow);
        var updatedAt = DateTime.UtcNow.AddMinutes(1);

        book.Update("The Clean Coder", "Robert C. Martin", "9780137081073", "Career", updatedAt);

        Assert.Equal("The Clean Coder", book.Title);
        Assert.Equal("Career", book.Category);
        Assert.Equal(updatedAt, book.UpdatedAtUtc);
    }
}
