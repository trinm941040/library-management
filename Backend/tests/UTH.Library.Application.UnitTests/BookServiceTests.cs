using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Features.Books;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using System.Text;

namespace UTH.Library.Application.UnitTests;

public sealed class BookServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_PersistsBook()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository, TimeProvider.System);

        var result = await service.CreateAsync(
            new CreateBookCommand("Clean Code", "Robert C. Martin", "9780132350884", "Programming"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Clean Code", result.Book?.Title);
        Assert.Single(repository.Books);
    }

    [Fact]
    public async Task CreateAsync_DuplicateIsbn_ReturnsConflict()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository, TimeProvider.System);
        await service.CreateAsync(
            new CreateBookCommand("Clean Code", "Robert C. Martin", "9780132350884", "Programming"),
            CancellationToken.None);

        var result = await service.CreateAsync(
            new CreateBookCommand("Another Title", "Another Author", "978-0132350884", "Programming"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(BookFailure.Conflict, result.Failure);
        Assert.Single(repository.Books);
    }

    [Fact]
    public async Task ImportPreview_PositiveQuantity_ReturnsRowError()
    {
        var service = new BookTransferService(new FakeBookRepository(), new FakeUnitOfWork(),
            new FakeRequestContext(), TimeProvider.System);
        var csv = Encoding.UTF8.GetBytes("Title,Author,ISBN,Category,Quantity\nClean Code,Robert C. Martin,9780132350884,Programming,2\n");

        var preview = await service.PreviewImportAsync(csv, CancellationToken.None);

        Assert.Contains(preview.Errors, error => error.Field == "quantity" && error.RowNumber == 2);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public void AddAuditLog(AuditLog auditLog) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken) => await operation(cancellationToken);
    }

    private sealed class FakeRequestContext : IRequestContext
    {
        public Guid? UserId => null;
        public string CorrelationId => "test";
        public string? IpAddress => null;
    }

    private sealed class FakeBookRepository : IBookRepository, IBookCatalogRepository
    {
        public List<Book> Books { get; } = [];

        public Task AddAsync(Book book, CancellationToken cancellationToken)
        {
            Books.Add(book);
            return Task.CompletedTask;
        }

        public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Books.SingleOrDefault(book => book.Id == id));

        public Task<bool> IsbnExistsAsync(string isbn, Guid? excludeId, CancellationToken cancellationToken) =>
            Task.FromResult(Books.Any(book => book.Isbn == isbn && (excludeId == null || book.Id != excludeId)));

        public Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
            string? search,
            string? category,
            int pageNumber,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken)
        {
            var items = Books.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<Book>, int)>((items, Books.Count));
        }

        public Task<IReadOnlyList<Book>> GetForExportAsync(string? search, string? category,
            string sortBy, bool descending, int maximumRows, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Book>>(Books.Take(maximumRows).ToArray());

        public Task<IReadOnlySet<string>> GetExistingIsbnsAsync(IEnumerable<string> isbns,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(Books.Select(book => book.Isbn).ToHashSet());

        public Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Remove(Book book) => Books.Remove(book);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(string? search, string? category,
            int pageNumber, int pageSize, IReadOnlyCollection<Guid>? authorIds, IReadOnlyCollection<Guid>? categoryIds,
            Guid? publisherId, RecordStatus? status, string sortBy, bool descending, CancellationToken cancellationToken) =>
            GetPageAsync(search, category, pageNumber, pageSize, sortBy, descending, cancellationToken);

        public Task<BookCatalogSnapshot> GetCatalogAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult(new BookCatalogSnapshot([], [], null, 0));

        public Task<IReadOnlyDictionary<Guid, BookCatalogSnapshot>> GetCatalogAsync(
            IReadOnlyCollection<Guid> bookIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BookCatalogSnapshot>>(
                bookIds.ToDictionary(id => id, _ => new BookCatalogSnapshot([], [], null, 0)));

        public Task<bool> ReferencesExistAsync(IReadOnlyCollection<Guid> authorIds, IReadOnlyCollection<Guid> categoryIds,
            Guid? publisherId, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task ReplaceRelationshipsAsync(Guid bookId, IReadOnlyCollection<Guid> authorIds,
            IReadOnlyCollection<Guid> categoryIds, Guid? publisherId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task NormalizeImportedBookAsync(Book book, string authorName, string categoryName,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task NormalizeBookAsync(Book book, string authorName, string categoryName,
            string? publisherName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasActiveDependenciesAsync(Guid bookId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<IReadOnlyList<BookCatalogReference>> SearchReferencesAsync(string type, string? search,
            int maximumResults, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookCatalogReference>>([]);
    }
}
