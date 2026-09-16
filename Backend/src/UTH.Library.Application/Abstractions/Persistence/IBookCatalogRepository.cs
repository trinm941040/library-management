using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IBookCatalogRepository
{
    Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPageAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        IReadOnlyCollection<Guid>? authorIds,
        IReadOnlyCollection<Guid>? categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken);

    Task<BookCatalogSnapshot> GetCatalogAsync(Guid bookId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, BookCatalogSnapshot>> GetCatalogAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken);

    Task<bool> ReferencesExistAsync(
        IReadOnlyCollection<Guid> authorIds,
        IReadOnlyCollection<Guid> categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken);

    Task ReplaceRelationshipsAsync(
        Guid bookId,
        IReadOnlyCollection<Guid> authorIds,
        IReadOnlyCollection<Guid> categoryIds,
        Guid? publisherId,
        CancellationToken cancellationToken);
}

public sealed record BookCatalogSnapshot(
    IReadOnlyCollection<BookCatalogReference> Authors,
    IReadOnlyCollection<BookCatalogReference> Categories,
    BookCatalogReference? Publisher,
    int AvailableCopyCount);

public sealed record BookCatalogReference(Guid Id, string Name);
