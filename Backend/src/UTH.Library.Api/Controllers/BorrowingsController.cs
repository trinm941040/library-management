using System.Security.Claims;
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
            GetCurrentUserId(),
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

    [HttpPost("{id:guid}/renew")]
    [Authorize(Policy = Permissions.BorrowingsCreate)]
    [ProducesResponseType(typeof(BorrowingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowingResponse>> Renew(
        Guid id,
        [FromBody] RenewBorrowingRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actorId))
            return Unauthorized();

        var result = await borrowingService.RenewAsync(
            id,
            new RenewBorrowingCommand(actorId, request.ConcurrencyToken),
            cancellationToken);
        return result.Succeeded && result.Borrowing is not null
            ? Ok(ToResponse(result.Borrowing))
            : MapFailure(result);
    }

    [HttpGet("checkout/lookup-member")]
    [Authorize(Policy = Permissions.BorrowingsCreate)]
    [ProducesResponseType(typeof(MemberCheckoutLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberCheckoutLookupResponse>> LookupMember(
        [FromQuery] string cardOrCode,
        CancellationToken cancellationToken)
    {
        var result = await borrowingService.LookupMemberForCheckoutAsync(cardOrCode, cancellationToken);
        if (result is null)
            return NotFound(CreateProblem("Không tìm thấy độc giả với mã thẻ hoặc mã độc giả đã nhập."));

        return Ok(new MemberCheckoutLookupResponse(
            result.MemberId,
            result.MemberCode,
            result.FullName,
            result.Email,
            result.MemberGroup,
            result.CardNumber,
            result.CardExpiresOn,
            result.CardStatus,
            result.MemberStatus,
            result.ActiveBorrowingsCount,
            result.BorrowingLimit,
            result.OverdueLoansCount,
            result.IsEligible,
            result.IneligibilityReasons));
    }

    [HttpGet("checkout/lookup-copy")]
    [Authorize(Policy = Permissions.BorrowingsCreate)]
    [ProducesResponseType(typeof(BookCopyCheckoutLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookCopyCheckoutLookupResponse>> LookupBookCopy(
        [FromQuery] string barcode,
        [FromQuery] Guid? memberId,
        CancellationToken cancellationToken)
    {
        var result = await borrowingService.LookupBookCopyForCheckoutAsync(barcode, memberId, cancellationToken);
        if (result is null)
            return NotFound(CreateProblem("Không tìm thấy bản sao sách với mã vạch đã quét."));

        return Ok(new BookCopyCheckoutLookupResponse(
            result.CopyId,
            result.BookId,
            result.Barcode,
            result.Title,
            result.Author,
            result.Isbn,
            result.Category,
            result.Condition,
            result.Status,
            result.IsAvailable,
            result.IneligibilityReason,
            result.PolicyName,
            result.LoanPeriodDays,
            result.SampleDueAtUtc));
    }

    [HttpPost("checkout")]
    [Authorize(Policy = Permissions.BorrowingsCreate)]
    [ProducesResponseType(typeof(BorrowingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowingResponse>> Checkout(
        [FromBody] CheckoutWithBarcodeRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var result = await borrowingService.CheckoutWithBarcodeAsync(
            new CheckoutWithBarcodeCommand(request.MemberCardOrCode, request.BookBarcode, request.LoanDaysOverride),
            actorId,
            cancellationToken);

        if (!result.Succeeded || result.Borrowing is null)
            return MapFailure(result);

        var response = ToResponse(result.Borrowing);
        return Created($"/api/v1/borrowings/{response.Id}", response);
    }

    [HttpGet("return/lookup-copy")]
    [Authorize(Policy = Permissions.BorrowingsReturn)]
    [ProducesResponseType(typeof(BookCopyReturnLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookCopyReturnLookupResponse>> LookupCopyForReturn(
        [FromQuery] string barcode,
        CancellationToken cancellationToken)
    {
        var result = await borrowingService.LookupCopyForReturnAsync(barcode, cancellationToken);
        if (result is null)
            return NotFound(CreateProblem("Không tìm thấy bản sao sách hoặc bản sao không có khoản mượn nào đang hoạt động."));

        return Ok(new BookCopyReturnLookupResponse(
            result.BorrowingId,
            result.BookId,
            result.Title,
            result.Author,
            result.CopyId,
            result.Barcode,
            result.Condition,
            result.BorrowerId,
            result.BorrowerName,
            result.BorrowerEmail,
            result.MemberCode,
            result.CardNumber,
            result.BorrowedAtUtc,
            result.DueAtUtc,
            result.IsOverdue,
            result.OverdueDays,
            result.FinePerDay,
            result.EstimatedOverdueFine,
            result.FixedDamageFine,
            result.EstimatedLostFine,
            result.PolicyName,
            result.ConcurrencyToken));
    }

    [HttpPost("return/confirm")]
    [Authorize(Policy = Permissions.BorrowingsReturn)]
    [ProducesResponseType(typeof(ReturnExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnExecutionResponse>> ConfirmReturn(
        [FromBody] ConfirmReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await borrowingService.ConfirmReturnAsync(
            new ConfirmReturnCommand(
                request.Barcode,
                request.Condition,
                request.Note,
                request.CustomDamageFine,
                request.CustomLostFine,
                request.ConcurrencyToken),
            cancellationToken);

        if (!result.Succeeded || result.Result is null)
            return MapReturnFailure(result);

        var data = result.Result;
        return Ok(new ReturnExecutionResponse(
            ToResponse(data.Borrowing),
            data.ReturnedAtUtc,
            data.Condition,
            data.Status,
            data.Violations.Select(v => new ViolationSummaryResponse(v.Id, v.Type, v.Note, v.FineAmount, v.RecordedAtUtc)).ToArray(),
            data.TotalFine,
            data.HasWaitingReservation));
    }

    private Guid GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    private ActionResult MapFailure(BorrowingResult result) => result.Failure switch
    {
        BorrowingFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Borrowing was not found.")),
        BorrowingFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Borrowing validation failed."))
    };

    private ActionResult MapReturnFailure(ReturnResult result) => result.Failure switch
    {
        BorrowingFailure.NotFound => NotFound(CreateProblem(result.Errors.FirstOrDefault() ?? "Book copy or borrowing was not found.")),
        BorrowingFailure.Conflict => Conflict(CreateProblem(result.Errors.FirstOrDefault() ?? "The operation conflicts with the current state.")),
        _ => BadRequest(CreateProblem(result.Errors.FirstOrDefault() ?? "Return validation failed."))
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
            borrowing.Status,
            borrowing.RenewalCount,
            borrowing.AppliedPolicyId,
            borrowing.AppliedPolicyVersion,
            borrowing.BookCopyId,
            borrowing.BookCopyBarcode,
            borrowing.ProcessedByEmployeeId);
}
