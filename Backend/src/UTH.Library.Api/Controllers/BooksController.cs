using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Books;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Books;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/books")]
public sealed class BooksController(BookService bookService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.BooksRead)]
    [ProducesResponseType(typeof(BookPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookPageResponse>> Get(
        [FromQuery] BookFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await bookService.GetAsync(
            new BookListQuery(request.Search, request.Category, request.PageNumber, request.PageSize),
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
        var result = await bookService.CreateAsync(
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
}
