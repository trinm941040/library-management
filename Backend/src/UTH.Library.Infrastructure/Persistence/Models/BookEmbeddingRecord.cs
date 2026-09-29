using Pgvector;

namespace UTH.Library.Infrastructure.Persistence.Models;

internal sealed class BookEmbeddingRecord
{
    public Guid BookId { get; set; }
    public Vector Embedding { get; set; } = null!;
    public string SourceHash { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
