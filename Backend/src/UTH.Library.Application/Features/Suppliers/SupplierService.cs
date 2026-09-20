using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Suppliers;

public sealed class SupplierService(ISupplierRepository repository, IUnitOfWork unitOfWork,
    IRequestContext requestContext, TimeProvider timeProvider)
{
    public async Task<PageResult<SupplierModel>> GetPageAsync(string? search, RecordStatus? status,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var (number, size) = CollectionLimits.NormalizePage(pageNumber, pageSize);
        var page = await repository.GetPageAsync(search, status, number, size, cancellationToken);
        return new PageResult<SupplierModel>(page.Items.Select(Map).ToArray(), number, size, page.TotalCount);
    }

    public async Task<SupplierModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(id, cancellationToken)) is { } snapshot ? Map(snapshot) : null;

    public async Task<IReadOnlyList<SupplierModel>> GetActiveAsync(CancellationToken cancellationToken) =>
        (await repository.GetActiveAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<SupplierModel> CreateAsync(SaveSupplierCommand command, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(command.Code);
        if (await repository.CodeExistsAsync(code, null, cancellationToken))
            throw new ResourceConflictException("Mã nhà cung cấp đã tồn tại.");
        Supplier supplier;
        try { supplier = Supplier.Create(code, command.Name, command.ContactName, command.Email, command.PhoneNumber, command.Address); }
        catch (ArgumentException exception) { throw Validation(exception.Message); }
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await repository.AddAsync(supplier, ct);
            Audit("supplier.created", supplier, null);
            return Map(supplier, false);
        }, cancellationToken);
    }

    public Task<SupplierModel> UpdateAsync(Guid id, SaveSupplierCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var supplier = await RequireAsync(id, ct);
            EnsureToken(command.ConcurrencyToken, supplier.ConcurrencyToken);
            var code = NormalizeCode(command.Code);
            if (await repository.CodeExistsAsync(code, id, ct))
                throw new ResourceConflictException("Mã nhà cung cấp đã tồn tại.");
            var before = JsonSerializer.Serialize(supplier);
            try { supplier.Update(code, command.Name, command.ContactName, command.Email, command.PhoneNumber, command.Address); }
            catch (ArgumentException exception) { throw Validation(exception.Message); }
            Audit("supplier.updated", supplier, before);
            return Map(supplier, await repository.HasReceiptsAsync(id, ct));
        }, cancellationToken);

    public Task<SupplierModel> ChangeStatusAsync(Guid id, RecordStatus status, Guid concurrencyToken, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var supplier = await RequireAsync(id, ct);
            EnsureToken(concurrencyToken, supplier.ConcurrencyToken);
            if (supplier.Status != status)
            {
                var before = JsonSerializer.Serialize(supplier);
                if (status == RecordStatus.Active) supplier.Activate();
                else if (status == RecordStatus.Inactive) supplier.Deactivate();
                else throw Validation("Trạng thái nhà cung cấp không hợp lệ.");
                Audit(status == RecordStatus.Active ? "supplier.activated" : "supplier.deactivated", supplier, before);
            }
            return Map(supplier, await repository.HasReceiptsAsync(id, ct));
        }, cancellationToken);

    private async Task<Supplier> RequireAsync(Guid id, CancellationToken ct) =>
        await repository.GetTrackedAsync(id, ct) ?? throw new ResourceNotFoundException("Không tìm thấy nhà cung cấp.");

    private void Audit(string action, Supplier supplier, string? before) =>
        unitOfWork.AddAuditLog(AuditLog.Create(requestContext.UserId, action, nameof(Supplier), supplier.Id,
            before, JsonSerializer.Serialize(supplier), timeProvider.GetUtcNow().UtcDateTime,
            requestContext.CorrelationId, requestContext.IpAddress));

    private static void EnsureToken(Guid? expected, Guid actual)
    {
        if (expected is null || expected == Guid.Empty) throw Validation("Thiếu phiên bản nhà cung cấp.");
        if (expected != actual) throw new OptimisticConcurrencyException("Nhà cung cấp đã được cập nhật. Vui lòng tải lại.");
    }

    private static string NormalizeCode(string code) => string.IsNullOrWhiteSpace(code)
        ? throw Validation("Mã nhà cung cấp không được để trống.") : code.Trim().ToUpperInvariant();
    private static RequestValidationException Validation(string message) =>
        new(new Dictionary<string, string[]> { ["supplier"] = [message] });
    private static SupplierModel Map(SupplierSnapshot snapshot) => Map(snapshot.Supplier, snapshot.HasStockReceipts);
    private static SupplierModel Map(Supplier supplier, bool hasReceipts) =>
        new(supplier.Id, supplier.Code, supplier.Name, supplier.ContactName, supplier.Email,
            supplier.PhoneNumber, supplier.Address, supplier.Status, supplier.ConcurrencyToken, hasReceipts);
}
