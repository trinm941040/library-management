namespace UTH.Library.Application.Features.Books;

public sealed record SemanticBookSearchQuery(string Query, Guid? CategoryId, bool AvailableOnly, int TopK = 10);
public sealed record SemanticBookSearchItem(Guid Id, string Title, string Author, string Isbn, string Category,
    string? Description, double Similarity, int TotalCopies, int AvailableCopies);
public sealed record SemanticBookSearchResult(IReadOnlyList<SemanticBookSearchItem> Items, string ScoreMeaning);
public sealed record EmbeddingBackfillResult(int ProcessedCount, int RemainingCount, bool HasMore);
