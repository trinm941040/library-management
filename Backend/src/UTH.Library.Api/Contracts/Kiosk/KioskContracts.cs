using System.ComponentModel.DataAnnotations;
using UTH.Library.Application.Features.Books;

namespace UTH.Library.Api.Contracts.Kiosk;

public sealed record KioskBookSearchRequest(
    [StringLength(200)] string? Search,
    [Range(1, int.MaxValue)] int PageNumber = 1,
    [Range(1, 20)] int PageSize = 20);

public sealed record KioskBookSummaryResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string? Description,
    int AvailableCopies);

public sealed record KioskBookPageResponse(
    IReadOnlyList<KioskBookSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record KioskSemanticSearchRequest(
    [Required, StringLength(SemanticBookSearchService.MaximumQueryLength, MinimumLength = 1)] string Query,
    Guid? CategoryId = null,
    bool AvailableOnly = false,
    [Range(1, SemanticBookSearchService.MaximumTopK)] int TopK = 10);

public sealed record KioskSemanticBookResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string? Description,
    double Similarity,
    int TotalCopies,
    int AvailableCopies);

public sealed record KioskSemanticSearchResponse(
    IReadOnlyList<KioskSemanticBookResponse> Items,
    string ScoreMeaning);

public sealed record KioskBookLocationResponse(string Label, int AvailableCopies);

public sealed record KioskBookDetailResponse(
    Guid Id,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string? Description,
    string? EditionStatement,
    int? PublicationYear,
    string? Language,
    int? PageCount,
    int TotalCopies,
    int AvailableCopies,
    IReadOnlyList<KioskBookLocationResponse> Locations);
