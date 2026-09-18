using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

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
        RecordStatus? status,
        string sortBy,
        bool descending,
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

    Task NormalizeImportedBookAsync(
        Book book,
        string authorName,
        string categoryName,
        CancellationToken cancellationToken);

    Task NormalizeBookAsync(
        Book book,
        string authorName,
        string categoryName,
        string? publisherName,
        CancellationToken cancellationToken);

    Task<bool> HasActiveDependenciesAsync(Guid bookId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookCatalogReference>> SearchReferencesAsync(
        string type,
        string? search,
        int maximumResults,
        CancellationToken cancellationToken);
}

public sealed record BookCatalogSnapshot(
    IReadOnlyCollection<BookCatalogReference> Authors,
    IReadOnlyCollection<BookCatalogReference> Categories,
    BookCatalogReference? Publisher,
    int AvailableCopyCount);

public sealed record BookCatalogReference(Guid Id, string Name);
