using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Reservations;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Reservations;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reservations")]
public sealed class ReservationsController(ReservationService reservationService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ReservationsRead)]
    [ProducesResponseType(typeof(ReservationPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationPageResponse>> Get(
        [FromQuery] ReservationFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await reservationService.GetAsync(
            new ReservationListQuery(request.Search, request.Status, request.PageNumber, request.PageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new ReservationPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ReservationsCreate)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reservationService.CreateAsync(
            new CreateReservationCommand(request.BookId, request.ReserverId, request.HoldDays),
            cancellationToken);

        if (!result.Succeeded || result.Reservation is null)
            return MapFailure(result);

        var response = ToResponse(result.Reservation);
        return Created($"/api/v1/reservations/{response.Id}", response);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.ReservationsCancel)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await reservationService.CancelAsync(id, cancellationToken);
        return result.Succeeded && result.Reservation is not null
            ? Ok(ToResponse(result.Reservation))
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy = Permissions.ReservationsFulfill)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Fulfill(Guid id, CancellationToken cancellationToken)
    {
        var result = await reservationService.FulfillAsync(id, cancellationToken);
        return result.Succeeded && result.Reservation is not null
            ? Ok(ToResponse(result.Reservation))
            : MapFailure(result);
    }

    private ActionResult MapFailure(ReservationResult result) => result.Failure switch
    {
        ReservationFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Reservation was not found.")),
        ReservationFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Reservation validation failed."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private static ReservationResponse ToResponse(ReservationModel reservation) =>
        new(
            reservation.Id,
            reservation.BookId,
            reservation.BookTitle,
            reservation.ReserverId,
            reservation.ReserverName,
            reservation.ReserverEmail,
            reservation.ReservedAtUtc,
            reservation.ExpiresAtUtc,
            reservation.FulfilledAtUtc,
            reservation.CancelledAtUtc,
            reservation.Status);
}
