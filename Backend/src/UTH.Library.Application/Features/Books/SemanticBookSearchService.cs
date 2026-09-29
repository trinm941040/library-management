using UTH.Library.Application.Abstractions.AI;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;

namespace UTH.Library.Application.Features.Books;

public sealed class SemanticBookSearchService(IEmbeddingService embeddingService,
    IBookSemanticSearchRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public const int MaximumTopK = 50;
    public const int MaximumQueryLength = 2000;
    public const int MaximumBackfillBatchSize = 100;

    public async Task<SemanticBookSearchResult> SearchAsync(SemanticBookSearchQuery query,
        CancellationToken cancellationToken)
    {
        Validate(query);
        var vector = await embeddingService.GenerateEmbeddingAsync(query.Query.Trim(), cancellationToken);
        EnsureDimensions(vector);
        var rows = await repository.SearchAsync(vector, query.CategoryId, query.AvailableOnly, query.TopK, cancellationToken);
        return new(rows.Select(row => new SemanticBookSearchItem(row.Id, row.Title, row.Author, row.Isbn,
                row.Category, row.Description, row.Similarity, row.TotalCopies, row.AvailableCopies)).ToArray(),
            "Mức liên quan ngữ nghĩa từ 0 đến 1, tính bằng max(0, 1 - cosine distance); không phải điểm chất lượng sách.");
    }

    public async Task<EmbeddingBackfillResult> BackfillAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > MaximumBackfillBatchSize)
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["batchSize"] = [$"Kích thước lô phải từ 1 đến {MaximumBackfillBatchSize}."]
            });
        var books = await repository.GetBooksWithoutEmbeddingsAsync(batchSize, cancellationToken);
        var generated = new List<(Guid BookId, ReadOnlyMemory<float> Vector, string Hash)>(books.Count);
        foreach (var book in books)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = BookSearchableTextBuilder.Build(book);
            var vector = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
            EnsureDimensions(vector);
            generated.Add((book.Id, vector, BookSearchableTextBuilder.Hash(text)));
        }
        if (generated.Count > 0)
        {
            await unitOfWork.ExecuteAsync(async ct =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                foreach (var item in generated)
                    await repository.UpsertEmbeddingAsync(item.BookId, item.Vector, item.Hash, now, ct);
                return generated.Count;
            }, cancellationToken);
        }
        var remaining = await repository.CountBooksWithoutEmbeddingsAsync(cancellationToken);
        return new(generated.Count, remaining, remaining > 0);
    }

    public static void Validate(SemanticBookSearchQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(query.Query)) errors["query"] = ["Nội dung tìm kiếm là bắt buộc."];
        else if (query.Query.Trim().Length > MaximumQueryLength) errors["query"] = [$"Nội dung tìm kiếm không được vượt quá {MaximumQueryLength} ký tự."];
        if (query.TopK is < 1 or > MaximumTopK) errors["topK"] = [$"topK phải từ 1 đến {MaximumTopK}."];
        if (query.CategoryId == Guid.Empty) errors["categoryId"] = ["Mã thể loại không hợp lệ."];
        if (errors.Count > 0) throw new RequestValidationException(errors);
    }

    private void EnsureDimensions(ReadOnlyMemory<float> vector)
    {
        if (vector.Length != embeddingService.Dimensions)
            throw new InvalidOperationException($"Embedding provider trả về {vector.Length} chiều, không khớp cấu hình {embeddingService.Dimensions} chiều.");
    }
}
