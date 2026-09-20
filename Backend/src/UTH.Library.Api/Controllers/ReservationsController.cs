using System.Security.Claims;
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

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ReservationsRead)]
    [ProducesResponseType(typeof(ReservationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await reservationService.GetDetailAsync(id, cancellationToken);
        if (detail is null)
            return NotFound(CreateProblem("Không tìm thấy thông tin đặt trước."));

        return Ok(ToDetailResponse(detail));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ReservationsCreate)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var result = await reservationService.CreateAsync(
            new CreateReservationCommand(request.BookId, request.ReserverId, request.HoldDays, actorId),
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Cancel(
        Guid id,
        [FromBody] CancelReservationRequest? request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var command = new CancelReservationCommand(
            actorId,
            request?.Reason,
            request?.ConcurrencyToken ?? Guid.Empty);

        var result = await reservationService.CancelAsync(id, command, cancellationToken);
        return result.Succeeded && result.Reservation is not null
            ? Ok(ToResponse(result.Reservation))
            : MapFailure(result);
    }

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Policy = Permissions.ReservationsFulfill)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Fulfill(
        Guid id,
        [FromBody] FulfillReservationRequest? request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var command = new FulfillReservationCommand(
            actorId,
            request?.BookCopyBarcode,
            request?.ConcurrencyToken ?? Guid.Empty);

        var result = await reservationService.FulfillAsync(id, command, cancellationToken);
        return result.Succeeded && result.Reservation is not null
            ? Ok(ToResponse(result.Reservation))
            : MapFailure(result);
    }

    private ActionResult MapFailure(ReservationResult result) => result.Failure switch
    {
        ReservationFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Không tìm thấy thông tin đặt trước.")),
        ReservationFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "Thao tác xung đột với trạng thái hiện tại của dữ liệu.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Dữ liệu yêu cầu không hợp lệ."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private Guid GetActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

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
            reservation.Status,
            reservation.AppliedPolicyId,
            reservation.AppliedPolicyVersion,
            reservation.QueuePosition,
            reservation.BookAuthor,
            reservation.BookCategory,
            reservation.ReserverMemberCode,
            reservation.ReserverCardNumber,
            reservation.ConcurrencyToken);

    private static ReservationDetailResponse ToDetailResponse(ReservationDetailModel detail) =>
        new(
            ToResponse(detail.Reservation),
            detail.BookIsbn,
            detail.ReserverGroup,
            detail.AvailableCopiesCount,
            detail.TotalActiveReservationsForBook,
            detail.PolicyName,
            detail.HoldDays,
            detail.BookQueue.Select(ToResponse).ToArray());
}
