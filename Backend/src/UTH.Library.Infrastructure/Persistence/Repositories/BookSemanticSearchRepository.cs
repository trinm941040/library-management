using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence.Models;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class BookSemanticSearchRepository(LibraryDbContext dbContext) : IBookSemanticSearchRepository
{
    public async Task UpsertEmbeddingAsync(Guid bookId, ReadOnlyMemory<float> embedding, string sourceHash,
        DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        var record = await dbContext.BookEmbeddings.FindAsync([bookId], cancellationToken);
        if (record is null)
        {
            await dbContext.BookEmbeddings.AddAsync(new BookEmbeddingRecord
            {
                BookId = bookId, Embedding = new Vector(embedding), SourceHash = sourceHash,
                UpdatedAtUtc = updatedAtUtc
            }, cancellationToken);
            return;
        }
        record.Embedding = new Vector(embedding);
        record.SourceHash = sourceHash;
        record.UpdatedAtUtc = updatedAtUtc;
    }

    public async Task<IReadOnlyList<SemanticBookSearchRow>> SearchAsync(ReadOnlyMemory<float> queryEmbedding,
        Guid? categoryId, bool availableOnly, int topK, CancellationToken cancellationToken)
    {
        var vector = new Vector(queryEmbedding);
        var query = from embedding in dbContext.BookEmbeddings.AsNoTracking()
                    join book in dbContext.Books.AsNoTracking() on embedding.BookId equals book.Id
                    where book.Status == RecordStatus.Active
                    let totalCopies = dbContext.BookCopies.Count(copy => copy.BookId == book.Id && copy.Status != CopyStatus.Withdrawn)
                    let availableCopies = dbContext.BookCopies.Count(copy => copy.BookId == book.Id && copy.Status == CopyStatus.Available)
                    where !availableOnly || availableCopies > 0
                    where categoryId == null || dbContext.BookCategories.Any(link =>
                        link.BookId == book.Id && link.CategoryId == categoryId)
                    let distance = embedding.Embedding.CosineDistance(vector)
                    orderby distance
                    select new SemanticBookSearchRow(book.Id, book.Title, book.Author, book.Isbn, book.Category,
                        book.Description, Math.Max(0d, 1d - distance), totalCopies, availableCopies);
        return await query.Take(topK).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> GetBooksWithoutEmbeddingsAsync(int batchSize,
        CancellationToken cancellationToken) => await dbContext.Books.AsNoTracking()
        .Where(book => book.Status == RecordStatus.Active &&
            !dbContext.BookEmbeddings.Any(embedding => embedding.BookId == book.Id))
        .OrderBy(book => book.Id).Take(batchSize).ToListAsync(cancellationToken);

    public Task<int> CountBooksWithoutEmbeddingsAsync(CancellationToken cancellationToken) =>
        dbContext.Books.AsNoTracking().CountAsync(book => book.Status == RecordStatus.Active &&
            !dbContext.BookEmbeddings.Any(embedding => embedding.BookId == book.Id), cancellationToken);
}
