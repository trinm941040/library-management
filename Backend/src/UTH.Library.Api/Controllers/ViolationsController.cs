using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Adjustments;
using UTH.Library.Api.Contracts.Violations;
using UTH.Library.Api.Contracts.Payments;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Payments;
using UTH.Library.Application.Features.Adjustments;
using UTH.Library.Application.Features.Violations;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/violations")]
public sealed class ViolationsController(
    ViolationService violationService,
    FinePaymentService paymentService,
    FineAdjustmentService adjustmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(ViolationPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ViolationPageResponse>> Get(
        [FromQuery] ViolationFilterRequest request,
        CancellationToken cancellationToken)
    {
        var page = await violationService.GetAsync(
            new ViolationListQuery(
                request.Search,
                request.BorrowerId,
                request.Type,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.HasBalanceOnly,
                request.PageNumber,
                request.PageSize),
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

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(ViolationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ViolationDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await violationService.GetDetailAsync(id, cancellationToken);
        if (detail is null)
            return NotFound(CreateProblem("Không tìm thấy thông tin vi phạm."));

        return Ok(ToDetailResponse(detail));
    }

    [HttpPost("preview")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FinePreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinePreviewResponse>> PreviewFine(
        [FromBody] FinePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var preview = await violationService.CalculateFinePreviewAsync(
            new FinePreviewCommand(
                request.BorrowerId,
                request.BookId,
                request.Type,
                request.OverdueDays,
                request.BookPrice,
                request.DamageLevel,
                request.CustomAmount),
            cancellationToken);

        return Ok(new FinePreviewResponse(
            preview.CalculatedFine,
            preview.Formula,
            preview.PolicyName,
            preview.DailyRate,
            preview.MaxFine,
            preview.LostRatio,
            preview.PolicyId,
            preview.PolicyVersion));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ViolationsCreate)]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ViolationResponse>> Create(
        [FromBody] CreateViolationRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var result = await violationService.CreateAsync(
            new CreateViolationCommand(
                request.BorrowerId,
                request.BookId,
                request.BookCopyId,
                request.BorrowingId,
                request.Type,
                request.Note ?? string.Empty,
                request.FineAmount,
                request.OverdueDays,
                request.BookPrice,
                request.DamageLevel,
                actorId),
            cancellationToken);

        if (!result.Succeeded || result.Violation is null)
            return MapFailure(result);

        var response = ToResponse(result.Violation);
        return Created($"/api/v1/violations/{response.Id}", response);
    }

    [HttpGet("{id:guid}/payment-preview")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FinePaymentPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinePaymentPreviewResponse>> GetPaymentPreview(Guid id, CancellationToken cancellationToken)
    {
        var preview = await paymentService.GetPreviewAsync(id, cancellationToken);
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

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = Permissions.ViolationsResolve)]
    [ProducesResponseType(typeof(ViolationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ViolationResponse>> Pay(
        Guid id,
        [FromBody] PayViolationRequest? request,
        CancellationToken cancellationToken)
    {
        var preview = await paymentService.GetPreviewAsync(id, cancellationToken);
        if (preview is null)
            return NotFound(CreateProblem("Không tìm thấy thông tin vi phạm."));

        if (!preview.IsOpen || preview.Balance <= 0)
            return Conflict(CreateProblem("Vi phạm này không còn số dư cần thanh toán."));

        var amount = request?.Amount ?? preview.Balance;
        var method = request?.Method ?? Domain.Entities.FinePaymentMethod.Cash;
        var actorId = GetActorUserId();
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Thủ thư";

        var result = await paymentService.CreatePaymentAsync(
            new CreateFinePaymentCommand(
                id,
                amount,
                method,
                request?.Reference,
                request?.IdempotencyKey,
                actorId != Guid.Empty ? actorId : null,
                actorName),
            cancellationToken);

        if (!result.Succeeded || result.Receipt is null)
        {
            return result.Failure switch
            {
                FinePaymentFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Không tìm thấy thông tin.")),
                FinePaymentFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "Thao tác xung đột với trạng thái hiện tại.")),
                _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Dữ liệu yêu cầu không hợp lệ."))
            };
        }

        Response.Headers.Append("X-Payment-Id", result.Receipt.Id.ToString());

        var updatedDetail = await violationService.GetDetailAsync(id, cancellationToken);
        return updatedDetail is not null
            ? Ok(ToResponse(updatedDetail.Violation))
            : Ok(new ViolationResponse(
                preview.ViolationId,
                preview.MemberId,
                preview.MemberName,
                preview.BorrowerEmail,
                null,
                preview.BookTitle,
                preview.ViolationType,
                string.Empty,
                preview.FineAmount,
                DateTime.UtcNow,
                result.Receipt.IsFullyPaid ? DateTime.UtcNow : null,
                result.Receipt.IsFullyPaid ? "paid" : "partially_paid",
                null,
                1,
                preview.TotalAdjusted,
                preview.TotalPaid + amount,
                result.Receipt.RemainingBalance,
                null, null, null,
                preview.MemberCode,
                null));
    }

    [HttpPost("{id:guid}/adjustment-preview")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(FineAdjustmentPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FineAdjustmentPreviewResponse>> GetAdjustmentPreview(
        Guid id,
        [FromBody] FineAdjustmentPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var preview = await adjustmentService.GetPreviewAsync(id, request.AmountDelta, cancellationToken);
        if (preview is null)
            return NotFound(CreateProblem("Không tìm thấy thông tin vi phạm."));

        return Ok(new FineAdjustmentPreviewResponse(
            preview.ViolationId,
            preview.OriginalFineAmount,
            preview.TotalAdjusted,
            preview.TotalPaid,
            preview.CurrentBalance,
            preview.AdjustmentAmountDelta,
            preview.ProjectedBalance,
            preview.ProjectedStatus,
            preview.IsAllowed,
            preview.ValidationMessage));
    }

    [HttpPost("{id:guid}/adjust")]
    [Authorize(Policy = Permissions.ViolationsAdjust)]
    [ProducesResponseType(typeof(FineAdjustmentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FineAdjustmentResultResponse>> Adjust(
        Guid id,
        [FromBody] CreateFineAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var result = await adjustmentService.AdjustAsync(
            new CreateFineAdjustmentCommand(id, request.AmountDelta, request.Reason, actorId),
            cancellationToken);

        if (!result.Succeeded)
            return MapAdjustmentFailure(result);

        return Ok(new FineAdjustmentResultResponse(
            true,
            result.Adjustment is not null ? ToAdjustmentResponse(result.Adjustment) : null,
            result.NewBalance,
            result.Status,
            []));
    }

    [HttpPost("{id:guid}/waive")]
    [Authorize(Policy = Permissions.ViolationsWaive)]
    [ProducesResponseType(typeof(FineAdjustmentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FineAdjustmentResultResponse>> Waive(
        Guid id,
        [FromBody] WaiveViolationRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetActorUserId();
        var result = await adjustmentService.WaiveAsync(
            new WaiveViolationCommand(id, request.Reason, actorId),
            cancellationToken);

        if (!result.Succeeded)
            return MapAdjustmentFailure(result);

        return Ok(new FineAdjustmentResultResponse(
            true,
            result.Adjustment is not null ? ToAdjustmentResponse(result.Adjustment) : null,
            result.NewBalance,
            result.Status,
            []));
    }

    [HttpGet("{id:guid}/adjustments")]
    [Authorize(Policy = Permissions.ViolationsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<FineAdjustmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FineAdjustmentResponse>>> GetAdjustments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var adjustments = await adjustmentService.GetAdjustmentsAsync(id, cancellationToken);
        return Ok(adjustments.Select(ToAdjustmentResponse).ToArray());
    }

    private ActionResult MapFailure(ViolationResult result) => result.Failure switch
    {
        ViolationFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Không tìm thấy vi phạm.")),
        ViolationFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "Thao tác xung đột với trạng thái hiện tại của dữ liệu.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Dữ liệu yêu cầu không hợp lệ."))
    };

    private ActionResult MapAdjustmentFailure(FineAdjustmentResult result) => result.Failure switch
    {
        FineAdjustmentFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Không tìm thấy vi phạm.")),
        FineAdjustmentFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "Dữ liệu xung đột với thao tác đồng thời.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Yêu cầu điều chỉnh không hợp lệ."))
    };

    private static ProblemDetails CreateProblem(string detail) => new() { Detail = detail };

    private Guid? GetActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

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
            violation.Status,
            violation.AppliedPolicyId,
            violation.AppliedPolicyVersion,
            violation.TotalAdjusted,
            violation.TotalPaid,
            violation.Balance,
            violation.BorrowingId,
            violation.BookCopyId,
            violation.BookCopyBarcode,
            violation.BorrowerMemberCode,
            violation.BorrowerCardNumber,
            violation.ConcurrencyToken);

    private static ViolationDetailResponse ToDetailResponse(ViolationDetailModel detail) =>
        new(
            ToResponse(detail.Violation),
            detail.BookIsbn,
            detail.BookAuthor,
            detail.BookCategory,
            detail.CalculationBasis,
            detail.Payments.Select(p => new PaymentHistoryItemResponse(
                p.Id,
                p.Amount,
                p.Method,
                p.Reference,
                p.PaidAtUtc,
                p.ReceivedByUserId)).ToArray(),
            detail.Adjustments.Select(a => new AdjustmentHistoryItemResponse(
                a.Id,
                a.AmountDelta,
                a.Reason,
                a.AdjustedAtUtc,
                a.AdjustedByUserId)).ToArray());

    private static FineAdjustmentResponse ToAdjustmentResponse(FineAdjustmentItemModel a) =>
        new(
            a.Id,
            a.MemberId,
            a.ViolationId,
            a.AmountDelta,
            a.Reason,
            a.AdjustedAtUtc,
            a.AdjustedByUserId);
}
