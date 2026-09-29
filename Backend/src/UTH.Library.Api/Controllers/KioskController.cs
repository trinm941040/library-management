using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UTH.Library.Api.Contracts.Kiosk;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Books;
using UTH.Library.Application.Features.Copies;
using UTH.Library.Application.Features.Locations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("kiosk-search")]
[Route("api/v1/kiosk")]
public sealed class KioskController(
    BookService bookService,
    CopyService copyService,
    LocationService locationService,
    SemanticBookSearchService semanticSearchService,
    IQueryHandler<BookListQuery, BookPageModel> listHandler) : ControllerBase
{
    [HttpGet("books")]
    public async Task<ActionResult<KioskBookPageResponse>> Search(
        [FromQuery] KioskBookSearchRequest request,
        CancellationToken cancellationToken)
    {
        var page = await listHandler.HandleAsync(
            new BookListQuery(request.Search, null, request.PageNumber, request.PageSize),
            cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new KioskBookPageResponse(
            page.Items.Select(MapSummary).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpPost("books/semantic-search")]
    [EnableRateLimiting("kiosk-semantic")]
    public async Task<ActionResult<KioskSemanticSearchResponse>> SemanticSearch(
        [FromBody] KioskSemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await semanticSearchService.SearchAsync(
            new SemanticBookSearchQuery(
                request.Query,
                request.CategoryId,
                request.AvailableOnly,
                request.TopK),
            cancellationToken);

        return Ok(new KioskSemanticSearchResponse(
            result.Items.Select(item => new KioskSemanticBookResponse(
                item.Id,
                item.Title,
                item.Author,
                item.Isbn,
                item.Category,
                item.Description,
                item.Similarity,
                item.TotalCopies,
                item.AvailableCopies)).ToArray(),
            result.ScoreMeaning));
    }

    [HttpGet("books/{id:guid}")]
    public async Task<ActionResult<KioskBookDetailResponse>> GetBook(
        Guid id,
        CancellationToken cancellationToken)
    {
        var book = await bookService.GetByIdAsync(id, cancellationToken);
        if (book is null || book.Status != RecordStatus.Active)
            return NotFound(new ProblemDetails { Title = "Không tìm thấy biểu ghi sách." });

        const int pageSize = CollectionLimits.MaximumPageSize;
        var firstPage = await copyService.GetPageAsync(
            new BookCopyQuery(null, id, null, null, null, null, 1, pageSize),
            cancellationToken);
        var copies = firstPage.Items.ToList();
        for (var pageNumber = 2; pageNumber <= firstPage.TotalPages; pageNumber++)
        {
            var nextPage = await copyService.GetPageAsync(
                new BookCopyQuery(null, id, null, null, null, null, pageNumber, pageSize),
                cancellationToken);
            copies.AddRange(nextPage.Items);
        }

        var shelves = await locationService.GetActiveShelvesAsync(cancellationToken);
        var shelfById = shelves.ToDictionary(shelf => shelf.Id);
        var availableCopies = copies.Where(copy => copy.Status == CopyStatus.Available).ToArray();
        var locations = availableCopies
            .GroupBy(copy => LocationLabel(copy, shelfById))
            .Select(group => new KioskBookLocationResponse(group.Key, group.Count()))
            .OrderBy(location => location.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return Ok(new KioskBookDetailResponse(
            book.Id,
            book.Title,
            book.Author,
            book.Isbn,
            book.Category,
            book.Description,
            book.EditionStatement,
            book.PublicationYear,
            book.Language,
            book.PageCount,
            copies.Count,
            availableCopies.Length,
            locations));
    }

    private static KioskBookSummaryResponse MapSummary(BookModel book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.Category,
        book.Description,
        book.AvailableCopyCount ?? 0);

    private static string LocationLabel(
        CopyModel copy,
        IReadOnlyDictionary<Guid, ShelfPickerModel> shelfById)
    {
        if (copy.ShelfId is { } shelfId && shelfById.TryGetValue(shelfId, out var shelf))
            return $"{shelf.BranchName} · {shelf.AreaName} · Kệ {shelf.Code}";

        return copy.ShelfCode is { Length: > 0 } shelfCode
            ? $"Kệ {shelfCode}"
            : "Chưa xếp vị trí";
    }
}
