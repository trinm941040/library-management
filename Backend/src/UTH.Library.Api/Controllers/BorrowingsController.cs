using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Borrowings;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Borrowings;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/borrowings")]
public sealed class BorrowingsController(BorrowingService borrowingService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.BorrowingsRead)]
    [ProducesResponseType(typeof(BorrowingPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BorrowingPageResponse>> Get(
        [FromQuery] BorrowingFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await borrowingService.GetAsync(
            new BorrowingListQuery(request.Search, request.Status, request.PageNumber, request.PageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new BorrowingPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.BorrowingsCreate)]
    [ProducesResponseType(typeof(BorrowingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowingResponse>> Create(
        [FromBody] CreateBorrowingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await borrowingService.CreateAsync(
            new CreateBorrowingCommand(request.BookId, request.BorrowerId, request.LoanDays),
            cancellationToken);

        if (!result.Succeeded || result.Borrowing is null)
            return MapFailure(result);

        var response = ToResponse(result.Borrowing);
        return Created($"/api/v1/borrowings/{response.Id}", response);
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Permissions.BorrowingsReturn)]
    [ProducesResponseType(typeof(BorrowingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BorrowingResponse>> Return(Guid id, CancellationToken cancellationToken)
    {
        var result = await borrowingService.ReturnAsync(id, cancellationToken);
        return result.Succeeded && result.Borrowing is not null
            ? Ok(ToResponse(result.Borrowing))
            : MapFailure(result);
    }

    private ActionResult MapFailure(BorrowingResult result) => result.Failure switch
    {
        BorrowingFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Borrowing was not found.")),
        BorrowingFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Borrowing validation failed."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private static BorrowingResponse ToResponse(BorrowingModel borrowing) =>
        new(
            borrowing.Id,
            borrowing.BookId,
            borrowing.BookTitle,
            borrowing.BorrowerId,
            borrowing.BorrowerName,
            borrowing.BorrowerEmail,
            borrowing.BorrowedAtUtc,
            borrowing.DueAtUtc,
            borrowing.ReturnedAtUtc,
            borrowing.Status);
}
