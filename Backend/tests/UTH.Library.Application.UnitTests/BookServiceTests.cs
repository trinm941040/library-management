using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Books;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.UnitTests;

public sealed class BookServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_PersistsBook()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository, TimeProvider.System);

        var result = await service.CreateAsync(
            new CreateBookCommand("Clean Code", "Robert C. Martin", "9780132350884", "Programming", 2),
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
            new CreateBookCommand("Clean Code", "Robert C. Martin", "9780132350884", "Programming", 2),
            CancellationToken.None);

        var result = await service.CreateAsync(
            new CreateBookCommand("Another Title", "Another Author", "978-0132350884", "Programming", 1),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(BookFailure.Conflict, result.Failure);
        Assert.Single(repository.Books);
    }

    private sealed class FakeBookRepository : IBookRepository
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
    }
}
