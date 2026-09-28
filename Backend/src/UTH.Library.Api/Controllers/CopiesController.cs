using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using UTH.Library.Api.Contracts.Copies;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Copies;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/copies")]
public sealed class CopiesController(CopyService service, IAuthorizationService authorization) : ControllerBase
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

    [HttpPost("bulk/{operation}")]
    public async Task<ActionResult<IReadOnlyList<CopyBulkResult>>> Bulk(string operation, CopyBulkRequest request, CancellationToken cancellationToken)
    {
        if (operation is not ("relocate" or "status" or "condition" or "withdraw")) return NotFound();
        if (!(await authorization.AuthorizeAsync(User, operation == "withdraw" ? Permissions.CopiesWithdraw : Permissions.CopiesUpdate)).Succeeded)
            return Forbid();
        var rows = request.Rows.Select(row => new CopyOperationRow(row.CopyId, row.ConcurrencyToken,
            row.Status, row.Condition, row.ShelfId, row.Reason)).ToArray();
        var result = await service.ApplyBulkAsync(rows, operation, cancellationToken);
        return Ok(result.Select(row => new CopyBulkResult(row.CopyId, row.Succeeded, row.Error,
            row.Copy is null ? null : Map(row.Copy))).ToArray());
    }

    [HttpPatch("{id:guid}/condition")]
    [Authorize(Policy = Permissions.CopiesUpdate)]
    public async Task<ActionResult<CopyResponse>> ChangeCondition(Guid id, ChangeCopyConditionRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeConditionAsync(id, new(request.Condition, request.ConcurrencyToken), cancellationToken)));

    [HttpPatch("{id:guid}/withdraw")]
    [Authorize(Policy = Permissions.CopiesWithdraw)]
    public async Task<ActionResult<CopyResponse>> Withdraw(Guid id, WithdrawCopyRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.WithdrawAsync(id, new(request.ConcurrencyToken, request.Reason), cancellationToken)));

    [HttpPost("import/preview")]
    [Authorize(Policy = Permissions.CopiesCreate)]
    public async Task<ActionResult<IReadOnlyList<CopyImportPreviewRow>>> ImportPreview(CopyImportRequest request, CancellationToken cancellationToken) =>
        Ok((await service.PreviewImportAsync(request.Rows.Select(MapImport).ToArray(), cancellationToken))
            .Select(row => new CopyImportPreviewRow(row.RowNumber, row.Barcode, row.Valid, row.Error)).ToArray());

    [HttpPost("import/confirm")]
    [Authorize(Policy = Permissions.CopiesCreate)]
    public async Task<ActionResult<IReadOnlyList<CopyResponse>>> ImportConfirm(CopyImportRequest request, CancellationToken cancellationToken) =>
        Ok((await service.ConfirmImportAsync(request.Rows.Select(MapImport).ToArray(), cancellationToken)).Select(Map).ToArray());

    [HttpGet("export")]
    [Authorize(Policy = Permissions.CopiesRead)]
    public async Task<IActionResult> Export([FromQuery] CopyFilterRequest request, CancellationToken cancellationToken)
    {
        var rows = await service.GetExportAsync(new BookCopyQuery(request.Search, request.BookId, request.BranchId,
            request.ShelfId, request.Condition, request.Status, 1, 100), cancellationToken);
        var csv = new StringBuilder("Barcode,BookTitle,ShelfCode,Condition,Status\r\n");
        foreach (var row in rows)
            csv.Append(Csv(row.Barcode)).Append(',').Append(Csv(row.BookTitle))
                .Append(',').Append(Csv(row.ShelfCode ?? ""))
                .Append(',').Append(row.Condition).Append(',').Append(row.Status).Append("\r\n");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "book-copies.csv");
    }

    private static ImportCopyRow MapImport(CopyImportRow row) => new(row.Barcode, row.Isbn, row.ShelfCode, row.Condition);
    private static string Csv(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@') value = "'" + value;
        return '"' + value.Replace("\"", "\"\"") + '"';
    }

    private static CopyResponse Map(CopyModel copy) => new(copy.Id, copy.BookId, copy.BookTitle,
        copy.Barcode, copy.Condition, copy.Status, copy.AcquiredAtUtc, copy.ShelfId, copy.ShelfCode,
        copy.BranchId, copy.BranchCode, copy.StockReceiptItemId, copy.ConcurrencyToken);
}
