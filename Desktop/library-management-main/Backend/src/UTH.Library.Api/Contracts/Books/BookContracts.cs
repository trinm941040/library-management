using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Books;

public sealed class BookFilterRequest
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public IReadOnlyCollection<Guid>? AuthorIds { get; init; }
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }
    public Guid? PublisherId { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateBookRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Required, StringLength(32, MinimumLength = 10)] string Isbn,
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    [Range(0, 100_000)] int Quantity,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null);

public sealed record UpdateBookRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Required, StringLength(32, MinimumLength = 10)] string Isbn,
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    [Range(0, 100_000)] int Quantity,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null);

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    int Quantity,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<BookReferenceResponse>? Authors = null,
    IReadOnlyCollection<BookReferenceResponse>? Categories = null,
    BookReferenceResponse? Publisher = null,
    int? AvailableCopyCount = null);

public sealed record BookReferenceResponse(Guid Id, string Name);

public sealed record BookPageResponse(
    IReadOnlyCollection<BookResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
