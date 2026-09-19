using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.UnitTests;

public sealed class BookCopyTests
{
    [Fact]
    public void Create_WithShelf_NormalizesBarcode()
    {
        var shelfId = Guid.NewGuid();
        var copy = BookCopy.Create(Guid.NewGuid(), "  bc-001  ", CopyCondition.Good, shelfId, null, DateTime.UtcNow);

        Assert.Equal("BC-001", copy.Barcode);
        Assert.Equal(shelfId, copy.ShelfId);
        Assert.Equal(CopyStatus.Available, copy.Status);
    }

    [Fact]
    public void Create_WithoutShelf_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BookCopy.Create(Guid.NewGuid(), "BC-002", CopyCondition.Good, Guid.Empty, null, DateTime.UtcNow));
    }

    [Fact]
    public void Relocate_BorrowedCopy_Throws()
    {
        var copy = BookCopy.Create(Guid.NewGuid(), "BC-003", CopyCondition.Good, Guid.NewGuid(), null, DateTime.UtcNow);
        copy.ChangeStatus(CopyStatus.Borrowed);

        Assert.Throws<InvalidOperationException>(() => copy.Relocate(Guid.NewGuid()));
    }
}
