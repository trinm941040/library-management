using System.ComponentModel.DataAnnotations;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Books;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Books;

public sealed class BookFilterRequest
{
    [StringLength(200)]
    public string? Search { get; init; }
    [StringLength(200)]
    public string? Category { get; init; }
    public IReadOnlyCollection<Guid>? AuthorIds { get; init; }
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }
    public Guid? PublisherId { get; init; }
    public RecordStatus? Status { get; init; } = RecordStatus.Active;

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [RegularExpression("^(title|author|isbn|category|createdAtUtc)$")]
    public string SortBy { get; init; } = "title";

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
}

public sealed record CreateBookRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Required, StringLength(32, MinimumLength = 10)] string Isbn,
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    [StringLength(200)] string? PublisherName = null,
    [StringLength(4000)] string? Description = null,
    [StringLength(200)] string? EditionStatement = null,
    [Range(0, 9999)] int? PublicationYear = null,
    [StringLength(100)] string? Language = null,
    [Range(1, 100000)] int? PageCount = null);

public sealed record UpdateBookRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(200, MinimumLength = 1)] string Author,
    [Required, StringLength(32, MinimumLength = 10)] string Isbn,
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    IReadOnlyCollection<Guid>? AuthorIds = null,
    IReadOnlyCollection<Guid>? CategoryIds = null,
    Guid? PublisherId = null,
    [StringLength(200)] string? PublisherName = null,
    [StringLength(4000)] string? Description = null,
    [StringLength(200)] string? EditionStatement = null,
    [Range(0, 9999)] int? PublicationYear = null,
    [StringLength(100)] string? Language = null,
    [Range(1, 100000)] int? PageCount = null,
    Guid? ConcurrencyToken = null);

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
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
    int? PublicationYear = null,
    string? Language = null,
    int? PageCount = null);

public sealed record BookReferenceResponse(Guid Id, string Name);

public sealed record BookPageResponse(
    IReadOnlyCollection<BookResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record BulkBookRequest(
    [MinLength(1), MaxLength(CollectionLimits.MaximumBulkItems)] IReadOnlyCollection<Guid> Ids);

public sealed record BookImportRowRequest(
    [Range(2, CollectionLimits.MaximumImportRows + 1)] int RowNumber,
    [Required, StringLength(200)] string Title,
    [Required, StringLength(200)] string Author,
    [Required, StringLength(32)] string Isbn,
    [Required, StringLength(100)] string Category,
    [StringLength(200)] string? Publisher = null,
    [StringLength(4000)] string? Description = null,
    [StringLength(200)] string? EditionStatement = null,
    [Range(0, 9999)] int? PublicationYear = null,
    [StringLength(100)] string? Language = null,
    [Range(1, 100000)] int? PageCount = null);

public sealed record ConfirmBookImportRequest(
    [MinLength(1), MaxLength(CollectionLimits.MaximumImportRows)] IReadOnlyList<BookImportRowRequest> Rows,
    [Required, StringLength(64, MinimumLength = 64)] string Checksum);

public sealed record ImportFieldErrorResponse(int RowNumber, string Field, string Message);
public sealed record BookImportPreviewResponse(
    IReadOnlyList<BookImportRow> Rows,
    IReadOnlyList<ImportFieldErrorResponse> Errors,
    string Checksum,
    bool CanConfirm);
public sealed record BookImportResultResponse(
    int ImportedCount,
    IReadOnlyList<ImportFieldErrorResponse> Errors,
    string CorrelationId);
public sealed record BulkItemResponse(Guid Id, bool Succeeded, string? Error);
public sealed record BulkResponse(
    IReadOnlyList<BulkItemResponse> Items,
    int SucceededCount,
    int FailedCount,
    string CorrelationId);

public sealed record SemanticBookSearchRequest(
    [Required, StringLength(SemanticBookSearchService.MaximumQueryLength, MinimumLength = 1)] string Query,
    Guid? CategoryId = null,
    bool AvailableOnly = false,
    [Range(1, SemanticBookSearchService.MaximumTopK)] int TopK = 10);
public sealed record SemanticBookSearchItemResponse(Guid Id, string Title, string Author, string Isbn,
    string Category, string? Description, double Similarity, int TotalCopies, int AvailableCopies);
public sealed record SemanticBookSearchResponse(IReadOnlyList<SemanticBookSearchItemResponse> Items, string ScoreMeaning);
public sealed record EmbeddingBackfillRequest(
    [Range(1, SemanticBookSearchService.MaximumBackfillBatchSize)] int BatchSize = 50);
public sealed record EmbeddingBackfillResponse(int ProcessedCount, int RemainingCount, bool HasMore);
