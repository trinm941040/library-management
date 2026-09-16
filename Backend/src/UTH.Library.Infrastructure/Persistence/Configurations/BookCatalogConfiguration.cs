using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Title).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Isbn).HasMaxLength(32).IsRequired();
        builder.Property(book => book.Category).HasMaxLength(100).IsRequired();
        builder.Property(book => book.Quantity).IsRequired();
        builder.Property(book => book.CreatedAtUtc).IsRequired();
        builder.Property(book => book.ConcurrencyToken)
            .IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(book => book.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RecordStatus.Active)
            .IsRequired();
        builder.Property(book => book.EditionStatement).HasMaxLength(200);
        builder.Property(book => book.Description).HasMaxLength(4000);
        builder.Property(book => book.Language).HasMaxLength(100);
        builder.HasIndex(book => book.Isbn).IsUnique();
        builder.HasIndex(book => book.Title);
        builder.HasIndex(book => book.Status);
        builder.HasOne<Publisher>()
            .WithMany()
            .HasForeignKey(book => book.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("book_authors");
        builder.HasKey(link => new { link.BookId, link.AuthorId });
        builder.HasOne<Book>().WithMany().HasForeignKey(link => link.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Author>().WithMany().HasForeignKey(link => link.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("book_categories");
        builder.HasKey(link => new { link.BookId, link.CategoryId });
        builder.HasOne<Book>().WithMany().HasForeignKey(link => link.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Category>().WithMany().HasForeignKey(link => link.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
