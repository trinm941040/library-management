using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Suppliers;

public sealed class SupplierFilterRequest
{
    public string? Search { get; init; }
    public RecordStatus? Status { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record SaveSupplierRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(200)] string Name,
    [StringLength(150)] string? ContactName,
    [EmailAddress, StringLength(256)] string? Email,
    [StringLength(32)] string? PhoneNumber,
    [StringLength(500)] string? Address,
    Guid? ConcurrencyToken = null);

public sealed record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? ContactName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    RecordStatus Status,
    Guid ConcurrencyToken,
    bool HasStockReceipts);

public sealed record SupplierPageResponse(IReadOnlyCollection<SupplierResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);
