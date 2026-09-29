using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.AI;
using UTH.Library.Infrastructure.Persistence.Models;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class BookEmbeddingConfiguration : IEntityTypeConfiguration<BookEmbeddingRecord>
{
    public void Configure(EntityTypeBuilder<BookEmbeddingRecord> builder)
    {
        builder.ToTable("book_embeddings");
        builder.HasKey(item => item.BookId);
        builder.Property(item => item.Embedding)
            .HasColumnType($"vector({AiEmbeddingOptions.DatabaseDimensions})")
            .IsRequired();
        builder.Property(item => item.SourceHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();
        builder.HasOne<Book>().WithOne().HasForeignKey<BookEmbeddingRecord>(item => item.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => item.SourceHash);
    }
}
