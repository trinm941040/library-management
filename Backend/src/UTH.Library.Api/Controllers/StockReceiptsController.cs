using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.StockReceipts;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.StockReceipts;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stock-receipts")]
public sealed class StockReceiptsController(StockReceiptService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.StockReceiptsRead)]
    public async Task<ActionResult<StockReceiptPageResponse>> Get(
        [FromQuery] StockReceiptFilterRequest request, CancellationToken cancellationToken)
    {
        var page = await service.GetPageAsync(new StockReceiptQuery(request.Search, request.FromUtc,
            request.ToUtc, request.SupplierId, request.BranchId, request.Status,
            request.PageNumber, request.PageSize), cancellationToken);
        return Ok(new StockReceiptPageResponse(page.Items.Select(Map).ToArray(), page.PageNumber,
            page.PageSize, page.TotalCount, page.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.StockReceiptsRead)]
    public async Task<ActionResult<StockReceiptResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        (await service.GetByIdAsync(id, cancellationToken)) is { } receipt ? Ok(Map(receipt)) : NotFound();

    [HttpPost]
    [Authorize(Policy = Permissions.StockReceiptsCreate)]
    public async Task<ActionResult<StockReceiptResponse>> Create(
        SaveStockReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await service.CreateAsync(ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, Map(receipt));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.StockReceiptsUpdate)]
    public async Task<ActionResult<StockReceiptResponse>> Update(
        Guid id, SaveStockReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateAsync(id, ToCommand(request), cancellationToken)));

    [HttpGet("{id:guid}/confirmation")]
    [Authorize(Policy = Permissions.StockReceiptsRead)]
    public async Task<ActionResult<ConfirmStockReceiptResponse>> GetConfirmation(Guid id,
        CancellationToken cancellationToken) =>
        (await service.GetConfirmationAsync(id, cancellationToken)) is { } result
            ? Ok(MapConfirmation(result)) : NotFound();

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Permissions.StockReceiptsConfirm)]
    public async Task<ActionResult<ConfirmStockReceiptResponse>> Confirm(Guid id,
        ConfirmStockReceiptRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(id, new ConfirmStockReceiptCommand(request.ConcurrencyToken,
            request.Items.Select(item => new ConfirmReceiptItemCommand(item.StockReceiptItemId,
                item.Copies.Select(copy => new ConfirmReceiptCopyCommand(copy.Barcode, copy.ShelfId,
                    copy.Condition)).ToArray())).ToArray()), cancellationToken);
        return Ok(MapConfirmation(result));
    }

    private static SaveStockReceiptCommand ToCommand(SaveStockReceiptRequest request) =>
        new(request.SupplierId, request.BranchId, request.ReceivedAtUtc, request.Notes,
            request.Items.Select(row => new SaveStockReceiptItemCommand(row.Id, row.BookId,
                row.ExpectedQuantity, row.ReceivedQuantity, row.DamagedQuantity, row.UnitCost)).ToArray(),
            request.ConcurrencyToken);

    private static StockReceiptResponse Map(StockReceiptModel receipt) =>
        new(receipt.Id, receipt.ReceiptNumber, receipt.SupplierId, receipt.SupplierName,
            receipt.BranchId, receipt.BranchCode, receipt.ReceivedByUserId, receipt.Status,
            receipt.ReceivedAtUtc, receipt.Notes, receipt.ConcurrencyToken,
            receipt.TotalQuantity, receipt.TotalValue,
            receipt.Items.Select(row => new StockReceiptItemResponse(row.Id, row.BookId,
                row.BookTitle, row.Isbn, row.ExpectedQuantity, row.ReceivedQuantity,
                row.DamagedQuantity, row.UnitCost, row.TotalValue, row.ConcurrencyToken)).ToArray());

    private static ConfirmStockReceiptResponse MapConfirmation(ConfirmStockReceiptResult result) =>
        new(Map(result.Receipt), result.Copies.Select(copy => new ReceiptCopyResponse(copy.Id,
            copy.StockReceiptItemId, copy.Barcode, copy.ShelfId, copy.Condition, copy.Status)).ToArray(),
            result.Discrepancies.Select(report => new DiscrepancyResponse(report.Id, report.Type,
                report.ExpectedQuantity, report.ActualQuantity, report.Description,
                report.CreatedAtUtc, report.CreatedByUserId)).ToArray());
}
