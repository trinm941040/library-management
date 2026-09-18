using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Suppliers;

public sealed record SupplierModel(Guid Id, string Code, string Name, string? ContactName, string? Email,
    string? PhoneNumber, string? Address, RecordStatus Status, Guid ConcurrencyToken, bool HasStockReceipts);

public sealed record SaveSupplierCommand(string Code, string Name, string? ContactName, string? Email,
    string? PhoneNumber, string? Address, Guid? ConcurrencyToken = null);
