using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IBookSemanticSearchRepository
{
    Task UpsertEmbeddingAsync(Guid bookId, ReadOnlyMemory<float> embedding, string sourceHash,
        DateTime updatedAtUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<SemanticBookSearchRow>> SearchAsync(ReadOnlyMemory<float> queryEmbedding,
        Guid? categoryId, bool availableOnly, int topK, CancellationToken cancellationToken);
    Task<IReadOnlyList<Book>> GetBooksWithoutEmbeddingsAsync(int batchSize, CancellationToken cancellationToken);
    Task<int> CountBooksWithoutEmbeddingsAsync(CancellationToken cancellationToken);
}

public sealed record SemanticBookSearchRow(Guid Id, string Title, string Author, string Isbn,
    string Category, string? Description, double Similarity, int TotalCopies, int AvailableCopies);
