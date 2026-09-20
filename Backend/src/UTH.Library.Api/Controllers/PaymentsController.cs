using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Payments;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Payments;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/payments")]
public sealed class PaymentsController(FinePaymentService paymentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FinePaymentPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinePaymentPageResponse>> Get(
        [FromQuery] PaymentFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await paymentService.GetPageAsync(
            new FinePaymentListQuery(
                request.Search,
                request.ViolationId,
                request.MemberId,
                request.Method,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize);

        return Ok(new FinePaymentPageResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            totalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FinePaymentReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinePaymentReceiptResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var receipt = await paymentService.GetReceiptAsync(id, cancellationToken);
        if (receipt is null)
            return NotFound(CreateProblem("Không tìm thấy biên nhận thanh toán."));

        return Ok(ToResponse(receipt));
    }

    [HttpPost("preview")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FinePaymentPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinePaymentPreviewResponse>> Preview(
        [FromBody] FinePaymentPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var preview = await paymentService.GetPreviewAsync(request.ViolationId, cancellationToken);
        if (preview is null)
            return NotFound(CreateProblem("Không tìm thấy thông tin vi phạm."));

        return Ok(new FinePaymentPreviewResponse(
            preview.ViolationId,
            preview.ViolationType,
            preview.BookTitle,
            preview.MemberId,
            preview.MemberName,
            preview.MemberCode,
            preview.BorrowerEmail,
            preview.FineAmount,
            preview.TotalAdjusted,
            preview.TotalPaid,
            preview.Balance,
            preview.SuggestedAmount,
            preview.IsOpen,
            preview.Status));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ViolationsResolve)]
    [ProducesResponseType(typeof(FinePaymentReceiptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FinePaymentReceiptResponse>> Create(
        [FromBody] CreateFinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Thủ thư";

        var result = await paymentService.CreatePaymentAsync(
            new CreateFinePaymentCommand(
                request.ViolationId,
                request.Amount,
                request.Method,
                request.Reference,
                request.IdempotencyKey,
                actorId != Guid.Empty ? actorId : null,
                actorName),
            cancellationToken);

        if (!result.Succeeded || result.Receipt is null)
            return MapFailure(result);

        var response = ToResponse(result.Receipt);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    private ActionResult MapFailure(FinePaymentResult result) => result.Failure switch
    {
        FinePaymentFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Không tìm thấy dữ liệu.")),
        FinePaymentFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "Thao tác xung đột với trạng thái hiện tại của dữ liệu.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Dữ liệu yêu cầu không hợp lệ."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private Guid GetActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static FinePaymentReceiptResponse ToResponse(FinePaymentReceiptModel receipt) =>
        new(
            receipt.Id,
            receipt.ViolationId,
            receipt.ViolationType,
            receipt.BookTitle,
            receipt.MemberId,
            receipt.MemberName,
            receipt.MemberCode,
            receipt.Amount,
            receipt.PreviousBalance,
            receipt.RemainingBalance,
            receipt.Method,
            receipt.Reference,
            receipt.PaidAtUtc,
            receipt.ReceivedByUserId,
            receipt.ReceivedByUserName,
            receipt.IsFullyPaid);
}
