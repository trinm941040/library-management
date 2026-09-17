using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Copies;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.UnitTests;

public sealed class CopyServiceTests
{
    [Fact]
    public async Task CreateAsync_UnconfirmedReceiptItem_IsRejectedBeforeInsert()
    {
        var repository = new FakeRepository { ReceiptItemIsConfirmed = false };
        var service = new CopyService(repository, new FakeUnitOfWork(), new FakeRequestContext(), TimeProvider.System);

        await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(
            new CreateCopyCommand(Guid.NewGuid(), "BC-100", CopyCondition.Good, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));

        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task CreateAsync_ConfirmedReceiptItem_PreservesLinkAndShelf()
    {
        var repository = new FakeRepository { ReceiptItemIsConfirmed = true };
        var service = new CopyService(repository, new FakeUnitOfWork(), new FakeRequestContext(), TimeProvider.System);
        var shelfId = Guid.NewGuid();
        var receiptItemId = Guid.NewGuid();

        var result = await service.CreateAsync(
            new CreateCopyCommand(Guid.NewGuid(), "bc-101", CopyCondition.Good, shelfId, receiptItemId),
            CancellationToken.None);

        Assert.Equal("BC-101", result.Barcode);
        Assert.Equal(shelfId, result.ShelfId);
        Assert.Equal(receiptItemId, result.StockReceiptItemId);
    }

    private sealed class FakeRepository : IBookCopyRepository
    {
        public bool ReceiptItemIsConfirmed { get; init; }
        public BookCopy? Added { get; private set; }
        public Task<PageResult<BookCopySnapshot>> GetPageAsync(BookCopyQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PageResult<BookCopySnapshot>([], 1, 20, 0));
        public Task<BookCopySnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Added is { } copy && copy.Id == id ?
                new BookCopySnapshot(copy, "Sách kiểm thử", "K1", Guid.NewGuid(), "CN1") : null);
        public Task<BookCopySnapshot?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
            Task.FromResult<BookCopySnapshot?>(null);
        public Task<BookCopy?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Added);
        public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ActiveBookExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ActiveShelfExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ReceiptItemMatchesBookAsync(Guid id, Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult(ReceiptItemIsConfirmed);
        public Task<bool> HasActiveAuditAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasEditableReceiptAsync(Guid? stockReceiptItemId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasActiveBorrowingForBookAsync(Guid bookId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddAsync(BookCopy copy, CancellationToken cancellationToken) { Added = copy; return Task.CompletedTask; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public void AddAuditLog(AuditLog auditLog) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken) => await operation(cancellationToken);
    }

    private sealed class FakeRequestContext : IRequestContext
    {
        public Guid? UserId => null;
        public string CorrelationId => "test";
        public string? IpAddress => null;
    }
}
