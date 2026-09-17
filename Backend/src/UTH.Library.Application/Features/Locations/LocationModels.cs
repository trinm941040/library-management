namespace UTH.Library.Application.Features.Locations;

public enum LocationType { Branch, Area, Shelf }

public sealed record LocationTreeModel(
    Guid Id,
    LocationType Type,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    Guid? ParentId,
    Guid ConcurrencyToken,
    BranchReadinessModel? Readiness,
    IReadOnlyList<LocationTreeModel> Children);

public sealed record BranchReadinessModel(
    bool CanActivate,
    int ActiveAreaCount,
    int ActiveShelfCount,
    IReadOnlyList<string> MissingRequirements);

public sealed record LocationImpactModel(
    int EmployeeCount,
    int BookCopyCount,
    int ActiveInventoryAuditCount,
    int EditableStockReceiptCount,
    bool HasBlockingReferences);

public sealed record ShelfPickerModel(
    Guid Id,
    string Code,
    string Label,
    Guid AreaId,
    string AreaCode,
    string AreaName,
    Guid BranchId,
    string BranchCode,
    string BranchName);

public sealed record SaveBranchCommand(
    string Code,
    string Name,
    string? Address,
    Guid? ConcurrencyToken = null);

public sealed record SaveAreaCommand(
    Guid BranchId,
    string Code,
    string Name,
    Guid? ConcurrencyToken = null);

public sealed record SaveShelfCommand(
    Guid AreaId,
    string Code,
    string Label,
    Guid? ConcurrencyToken = null);

public sealed record ChangeLocationStatusCommand(bool IsActive, Guid ConcurrencyToken);
