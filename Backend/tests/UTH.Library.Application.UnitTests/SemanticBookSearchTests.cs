using UTH.Library.Application.Abstractions.AI;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Books;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.UnitTests;

public sealed class SemanticBookSearchTests
{
    [Fact]
    public void SearchableText_UsesOnlyCanonicalSearchFields()
    {
        var text = BookSearchableTextBuilder.Build(" Clean Architecture ", " Robert C. Martin ",
            " Software Architecture ", " Design principles ");

        Assert.Equal("Title: Clean Architecture\nAuthor: Robert C. Martin\nCategory: Software Architecture\nDescription: Design principles", text);
        Assert.False(BookSearchableTextBuilder.HasChanged(text, text));
        Assert.True(BookSearchableTextBuilder.HasChanged(text, text + "."));
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("architecture", 0)]
    [InlineData("architecture", 51)]
    public void Validate_InvalidRequest_Throws(string query, int topK)
    {
        Assert.Throws<RequestValidationException>(() =>
            SemanticBookSearchService.Validate(new SemanticBookSearchQuery(query, null, false, topK)));
    }

    [Fact]
    public async Task SearchAsync_UsesEmbeddingAndRepositoryProjection()
    {
        var embedding = new FakeEmbeddingService();
        var repository = new FakeSemanticRepository();
        var service = new SemanticBookSearchService(embedding, repository, new FakeUnitOfWork(), TimeProvider.System);

        var result = await service.SearchAsync(
            new SemanticBookSearchQuery("backend architecture", null, true, 8), CancellationToken.None);

        Assert.Equal("backend architecture", embedding.LastText);
        Assert.Equal(8, repository.LastTopK);
        Assert.True(repository.LastAvailableOnly);
        Assert.Single(result.Items);
        Assert.Equal(0.91, result.Items[0].Similarity);
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public int Dimensions => 3;
        public string? LastText { get; private set; }
        public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            LastText = text;
            return Task.FromResult<ReadOnlyMemory<float>>(new float[] { 1, 0, 0 });
        }
    }

    private sealed class FakeSemanticRepository : IBookSemanticSearchRepository
    {
        public int LastTopK { get; private set; }
        public bool LastAvailableOnly { get; private set; }
        public Task<IReadOnlyList<SemanticBookSearchRow>> SearchAsync(ReadOnlyMemory<float> queryEmbedding,
            Guid? categoryId, bool availableOnly, int topK, CancellationToken cancellationToken)
        {
            LastTopK = topK;
            LastAvailableOnly = availableOnly;
            return Task.FromResult<IReadOnlyList<SemanticBookSearchRow>>([
                new(Guid.NewGuid(), "Clean Architecture", "Robert C. Martin", "9780134494166",
                    "Architecture", null, 0.91, 8, 3)
            ]);
        }
        public Task UpsertEmbeddingAsync(Guid bookId, ReadOnlyMemory<float> embedding, string sourceHash,
            DateTime updatedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Book>> GetBooksWithoutEmbeddingsAsync(int batchSize,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task<int> CountBooksWithoutEmbeddingsAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public void AddAuditLog(AuditLog auditLog) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken) => await operation(cancellationToken);
    }
}
