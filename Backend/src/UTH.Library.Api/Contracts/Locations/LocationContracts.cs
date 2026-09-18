using System.ComponentModel.DataAnnotations;
using UTH.Library.Application.Features.Locations;

namespace UTH.Library.Api.Contracts.Locations;

public sealed record SaveBranchRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Name,
    [StringLength(500)] string? Address,
    Guid? ConcurrencyToken = null);

public sealed record SaveAreaRequest(
    Guid BranchId,
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Name,
    Guid? ConcurrencyToken = null);

public sealed record SaveShelfRequest(
    Guid AreaId,
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Label,
    Guid? ConcurrencyToken = null);

public sealed record ChangeLocationStatusRequest(Guid ConcurrencyToken);

public sealed record BranchReadinessResponse(
    bool CanActivate,
    int ActiveAreaCount,
    int ActiveShelfCount,
    IReadOnlyList<string> MissingRequirements);

public sealed record LocationResponse(
    Guid Id,
    LocationType Type,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    Guid? ParentId,
    Guid ConcurrencyToken,
    BranchReadinessResponse? Readiness,
    IReadOnlyList<LocationResponse> Children);

public sealed record LocationImpactResponse(
    int EmployeeCount,
    int BookCopyCount,
    int ActiveInventoryAuditCount,
    int EditableStockReceiptCount,
    bool HasBlockingReferences);

public sealed record ShelfPickerResponse(
    Guid Id,
    string Code,
    string Label,
    Guid AreaId,
    string AreaCode,
    string AreaName,
    Guid BranchId,
    string BranchCode,
    string BranchName);
