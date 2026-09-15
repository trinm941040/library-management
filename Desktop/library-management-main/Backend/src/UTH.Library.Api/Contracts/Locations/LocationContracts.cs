using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Locations;

public sealed record SaveBranchRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Name,
    [StringLength(500)] string? Address);

public sealed record SaveAreaRequest(
    Guid BranchId,
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Name);

public sealed record SaveShelfRequest(
    Guid AreaId,
    [Required, StringLength(30)] string Code,
    [Required, StringLength(150)] string Label);

public sealed record LocationResponse(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    Guid? BranchId,
    Guid? AreaId,
    ShelfStatus? Status,
    Guid ConcurrencyToken,
    IReadOnlyCollection<LocationResponse>? Children = null);
