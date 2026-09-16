using System.ComponentModel.DataAnnotations;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Books;

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

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [RegularExpression("^(title|author|isbn|category|quantity|createdAtUtc)$")]
    public string SortBy { get; init; } = "title";

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
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

public sealed record BulkBookRequest(
    [MinLength(1), MaxLength(CollectionLimits.MaximumBulkItems)] IReadOnlyCollection<Guid> Ids);

public sealed record BookImportRowRequest(
    [Range(2, CollectionLimits.MaximumImportRows + 1)] int RowNumber,
    [Required, StringLength(200)] string Title,
    [Required, StringLength(200)] string Author,
    [Required, StringLength(32)] string Isbn,
    [Required, StringLength(100)] string Category,
    [Range(0, 100_000)] int Quantity);

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
