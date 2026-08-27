using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Violations;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Violations;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/violations")]
public sealed class ViolationsController(ViolationService violationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(ViolationPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ViolationPageResponse>> Get(
        [FromQuery] ViolationFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await violationService.GetAsync(
            new ViolationListQuery(request.Search, request.Status, request.PageNumber, request.PageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new ViolationPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ViolationsCreate)]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ViolationResponse>> Create(
        [FromBody] CreateViolationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await violationService.CreateAsync(
            new CreateViolationCommand(request.BorrowerId, request.BookId, request.Type, request.Note ?? string.Empty, request.FineAmount),
            cancellationToken);

        if (!result.Succeeded || result.Violation is null)
            return MapFailure(result);

        var response = ToResponse(result.Violation);
        return Created($"/api/v1/violations/{response.Id}", response);
    }

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = Permissions.ViolationsResolve)]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ViolationResponse>> Pay(Guid id, CancellationToken cancellationToken)
    {
        var result = await violationService.PayAsync(id, cancellationToken);
        return result.Succeeded && result.Violation is not null
            ? Ok(ToResponse(result.Violation))
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/waive")]
    [Authorize(Policy = Permissions.ViolationsResolve)]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ViolationResponse>> Waive(Guid id, CancellationToken cancellationToken)
    {
        var result = await violationService.WaiveAsync(id, cancellationToken);
        return result.Succeeded && result.Violation is not null
            ? Ok(ToResponse(result.Violation))
            : MapFailure(result);
    }

    private ActionResult MapFailure(ViolationResult result) => result.Failure switch
    {
        ViolationFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Violation was not found.")),
        ViolationFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Violation validation failed."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private static ViolationResponse ToResponse(ViolationModel violation) =>
        new(
            violation.Id,
            violation.BorrowerId,
            violation.BorrowerName,
            violation.BorrowerEmail,
            violation.BookId,
            violation.BookTitle,
            violation.Type,
            violation.Note,
            violation.FineAmount,
            violation.RecordedAtUtc,
            violation.ResolvedAtUtc,
            violation.Status);
}
