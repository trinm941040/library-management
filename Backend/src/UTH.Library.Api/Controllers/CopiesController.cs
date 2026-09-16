using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Copies;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Copies;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/copies")]
public sealed class CopiesController(CopyService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyPageResponse>> Get([FromQuery] CopyFilterRequest request, CancellationToken cancellationToken)
    {
        var page = await service.GetPageAsync(new BookCopyQuery(request.Search, request.BookId, request.BranchId,
            request.ShelfId, request.Condition, request.Status, request.PageNumber, request.PageSize), cancellationToken);
        return Ok(new CopyPageResponse(page.Items.Select(Map).ToArray(), page.PageNumber,
            page.PageSize, page.TotalCount, page.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var copy = await service.GetByIdAsync(id, cancellationToken);
        return copy is null ? NotFound() : Ok(Map(copy));
    }

    [HttpGet("barcode/{barcode}")]
    [Authorize(Policy = Permissions.CopiesRead)]
    public async Task<ActionResult<CopyResponse>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var copy = await service.GetByBarcodeAsync(barcode, cancellationToken);
        return copy is null ? NotFound() : Ok(Map(copy));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CopiesCreate)]
    public async Task<ActionResult<CopyResponse>> Create(CreateCopyRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(new CreateCopyCommand(request.BookId, request.Barcode,
            request.Condition, request.ShelfId, request.StockReceiptItemId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.CopiesUpdate)]
    public async Task<ActionResult<CopyResponse>> ChangeStatus(Guid id, ChangeCopyStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeStatusAsync(id, new ChangeCopyStatusCommand(request.Status, request.ConcurrencyToken), cancellationToken)));

    [HttpPatch("{id:guid}/relocate")]
    [Authorize(Policy = Permissions.CopiesUpdate)]
    public async Task<ActionResult<CopyResponse>> Relocate(Guid id, RelocateCopyRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.RelocateAsync(id, new RelocateCopyCommand(request.ShelfId, request.ConcurrencyToken), cancellationToken)));

    private static CopyResponse Map(CopyModel copy) => new(copy.Id, copy.BookId, copy.BookTitle,
        copy.Barcode, copy.Condition, copy.Status, copy.AcquiredAtUtc, copy.ShelfId, copy.ShelfCode,
        copy.BranchId, copy.BranchCode, copy.StockReceiptItemId, copy.ConcurrencyToken);
}
