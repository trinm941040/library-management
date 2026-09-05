using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class BookTests
{
    [Fact]
    public void Create_EmptyTitle_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Book.Create(" ", "Author", "9780132350884", "Programming", 1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_NegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Book.Create("Clean Code", "Robert C. Martin", "9780132350884", "Programming", -1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_ValidValues_NormalizesIsbn()
    {
        var book = Book.Create("Clean Code", "Robert C. Martin", "978-0-13-235088-4", "Programming", 3, DateTime.UtcNow);

        Assert.Equal("9780132350884", book.Isbn);
        Assert.Equal("Clean Code", book.Title);
        Assert.Equal(3, book.Quantity);
    }

    [Fact]
    public void Update_ValidValues_ChangesFields()
    {
        var book = Book.Create("Clean Code", "Robert C. Martin", "9780132350884", "Programming", 3, DateTime.UtcNow);
        var updatedAt = DateTime.UtcNow.AddMinutes(1);

        book.Update("The Clean Coder", "Robert C. Martin", "9780137081073", "Career", 5, updatedAt);

        Assert.Equal("The Clean Coder", book.Title);
        Assert.Equal("Career", book.Category);
        Assert.Equal(5, book.Quantity);
        Assert.Equal(updatedAt, book.UpdatedAtUtc);
    }

    [Fact]
    public void Checkout_reduces_quantity()
    {
        var book = Book.Create("Refactoring", "Martin Fowler", "9780201485677", "Software", 2, DateTime.UtcNow);

        book.Checkout(DateTime.UtcNow);

        Assert.Equal(1, book.Quantity);
    }

    [Fact]
    public void Checkout_throws_when_no_copies_are_available()
    {
        var book = Book.Create("Refactoring", "Martin Fowler", "9780201485677", "Software", 1, DateTime.UtcNow);
        book.Checkout(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => book.Checkout(DateTime.UtcNow));
    }
}
