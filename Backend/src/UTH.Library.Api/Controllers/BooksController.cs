using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Books;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Books;
using UTH.Library.Application.Common;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/books")]
public sealed class BooksController(
    BookService bookService,
    BookTransferService transferService,
    IQueryHandler<BookListQuery, BookPageModel> listHandler,
    ICommandHandler<CreateBookCommand, BookResult> createHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.BooksRead)]
    [ProducesResponseType(typeof(BookPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookPageResponse>> Get(
        [FromQuery] BookFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await listHandler.HandleAsync(
            new BookListQuery(request.Search, request.Category, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new BookPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpGet("export")]
    [Authorize(Policy = Permissions.BooksRead)]
    [Produces("text/csv")]
    public async Task<IActionResult> Export([FromQuery] BookFilterRequest request, CancellationToken cancellationToken)
    {
        var stream = await transferService.ExportAsync(
            new BookListQuery(request.Search, request.Category, 1, CollectionLimits.MaximumPageSize, request.SortBy, request.SortDirection),
            cancellationToken);
        return File(stream, "text/csv; charset=utf-8", $"books-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpPost("import/preview")]
    [Authorize(Policy = Permissions.BooksCreate)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<BookImportPreviewResponse>> PreviewImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > 2 * 1024 * 1024 ||
            !string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return UnprocessableEntity(CreateProblem("Tệp phải là CSV không rỗng và không vượt quá 2 MB."));
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        var preview = await transferService.PreviewImportAsync(buffer.ToArray(), cancellationToken);
        return Ok(new BookImportPreviewResponse(preview.Rows, preview.Errors.Select(Map).ToArray(), preview.Checksum, preview.CanConfirm));
    }

    [HttpPost("import/confirm")]
    [Authorize(Policy = Permissions.BooksCreate)]
    public async Task<ActionResult<BookImportResultResponse>> ConfirmImport(
        ConfirmBookImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transferService.ConfirmImportAsync(new ConfirmBookImportCommand(
            request.Rows.Select(row => new BookImportRow(row.RowNumber, row.Title, row.Author, row.Isbn, row.Category, row.Quantity)).ToArray(),
            request.Checksum), cancellationToken);
        return Ok(new BookImportResultResponse(result.ImportedCount, result.Errors.Select(Map).ToArray(), result.CorrelationId));
    }

    [HttpPost("bulk-delete")]
    [Authorize(Policy = Permissions.BooksDelete)]
    public async Task<ActionResult<BulkResponse>> BulkDelete(BulkBookRequest request, CancellationToken cancellationToken)
    {
        var result = await transferService.DeleteBulkAsync(request.Ids, cancellationToken);
        return Ok(new BulkResponse(result.Items.Select(item => new BulkItemResponse(item.Id, item.Succeeded, item.Error)).ToArray(),
            result.SucceededCount, result.FailedCount, result.CorrelationId));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.BooksRead)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var book = await bookService.GetByIdAsync(id, cancellationToken);
        return book is null ? NotFound(CreateProblem("Book was not found.")) : Ok(ToResponse(book));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.BooksCreate)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Create(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateBookCommand(request.Title, request.Author, request.Isbn, request.Category, request.Quantity),
            cancellationToken);

        if (!result.Succeeded || result.Book is null)
            return MapFailure(result);

        var response = ToResponse(result.Book);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.BooksUpdate)]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Update(
        Guid id,
        [FromBody] UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookService.UpdateAsync(
            id,
            new UpdateBookCommand(request.Title, request.Author, request.Isbn, request.Category, request.Quantity),
            cancellationToken);

        return result.Succeeded && result.Book is not null
            ? Ok(ToResponse(result.Book))
            : MapFailure(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.BooksDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await bookService.DeleteAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : MapFailure(result);
    }

    private ActionResult MapFailure(BookResult result) => result.Failure switch
    {
        BookFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Book was not found.")),
        BookFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Book validation failed."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private static BookResponse ToResponse(BookModel book) =>
        new(book.Id, book.Title, book.Author, book.Isbn, book.Category, book.Quantity, book.CreatedAtUtc, book.UpdatedAtUtc);

    private static ImportFieldErrorResponse Map(ImportFieldError error) =>
        new(error.RowNumber, error.Field, error.Message);
}
