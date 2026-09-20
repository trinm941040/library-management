using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Suppliers;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Suppliers;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/suppliers")]
public sealed class SuppliersController(SupplierService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<SupplierPageResponse>> Get([FromQuery] SupplierFilterRequest request, CancellationToken cancellationToken)
    {
        var page = await service.GetPageAsync(request.Search, request.Status, request.PageNumber, request.PageSize, cancellationToken);
        return Ok(new SupplierPageResponse(page.Items.Select(Map).ToArray(), page.PageNumber, page.PageSize,
            page.TotalCount, page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)page.PageSize)));
    }

    [HttpGet("active")]
    [Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetActive(CancellationToken cancellationToken) =>
        Ok((await service.GetActiveAsync(cancellationToken)).Select(Map).ToArray());

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<SupplierResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        (await service.GetByIdAsync(id, cancellationToken)) is { } supplier ? Ok(Map(supplier)) : NotFound();

    [HttpPost]
    [Authorize(Policy = Permissions.SuppliersCreate)]
    public async Task<ActionResult<SupplierResponse>> Create(SaveSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await service.CreateAsync(Command(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, Map(supplier));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SuppliersUpdate)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.UpdateAsync(id, Command(request), cancellationToken)));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.SuppliersUpdate)]
    public async Task<ActionResult<SupplierResponse>> Activate(Guid id, ChangeSupplierStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeStatusAsync(id, RecordStatus.Active, request.ConcurrencyToken, cancellationToken)));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.SuppliersDeactivate)]
    public async Task<ActionResult<SupplierResponse>> Deactivate(Guid id, [FromBody] ChangeSupplierStatusRequest request, CancellationToken cancellationToken) =>
        Ok(Map(await service.ChangeStatusAsync(id, RecordStatus.Inactive, request.ConcurrencyToken, cancellationToken)));

    private static SaveSupplierCommand Command(SaveSupplierRequest request) =>
        new(request.Code, request.Name, request.ContactName, request.Email, request.PhoneNumber, request.Address, request.ConcurrencyToken);
    private static SupplierResponse Map(SupplierModel supplier) =>
        new(supplier.Id, supplier.Code, supplier.Name, supplier.ContactName, supplier.Email,
            supplier.PhoneNumber, supplier.Address, supplier.Status, supplier.ConcurrencyToken, supplier.HasStockReceipts);
}
