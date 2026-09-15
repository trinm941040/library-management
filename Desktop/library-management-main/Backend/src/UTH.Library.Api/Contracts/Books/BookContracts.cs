using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Books;

public sealed class BookFilterRequest
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public IReadOnlyCollection<Guid>? AuthorIds { get; init; }
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }
    public Guid? PublisherId { get; init; }
    public RecordStatus? Status { get; init; }
    public string SortBy { get; init; } = "title";
    public string SortDirection { get; init; } = "asc";

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
    Guid? PublisherId = null,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null);

public sealed record UpdateBookRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Required, StringLength(32, MinimumLength = 10)] string Isbn,
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    [Range(0, 100_000)] int Quantity,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null,
    Guid? ConcurrencyToken = null);

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
    int? AvailableCopyCount = null,
    RecordStatus Status = RecordStatus.Active,
    Guid ConcurrencyToken = default,
    string? Description = null,
    string? EditionStatement = null,
    int? PublicationYear = null);

public sealed record BookReferenceResponse(Guid Id, string Name);

public sealed record BookPageResponse(
    IReadOnlyCollection<BookResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
