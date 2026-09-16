using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.AccessAccounts;

public sealed class AccessAccountFilterRequest
{
    [StringLength(200)] public string? Search { get; init; }
    public bool? IsActive { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record CreateAccessAccountRequest(
    Guid EmployeeId,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName,
    [Required, StringLength(256, MinimumLength = 8)] string Password,
    [Required, MinLength(1)] IReadOnlyCollection<Guid> RoleIds);

public sealed record SetAccessAccountStatusRequest(bool IsActive);
public sealed record ResetAccessAccountPasswordRequest(
    [Required, StringLength(256, MinimumLength = 8)] string TemporaryPassword);
public sealed record ReplaceAccessAccountRolesRequest(
    [Required, MinLength(1)] IReadOnlyCollection<Guid> RoleIds);

public sealed record AccessAccountEmployeeResponse(
    Guid Id, string EmployeeCode, string FullName, string Email, string EmploymentStatus);
public sealed record EligibleAccessAccountEmployeeResponse(
    Guid Id, string EmployeeCode, string FullName, string Email);
public sealed record AccessAccountResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    AccessAccountEmployeeResponse Employee,
    bool IsProtected);
public sealed record AccessAccountPageResponse(
    IReadOnlyCollection<AccessAccountResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
public sealed record AccessAccountSessionResponse(
    Guid Id,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? UsedAtUtc,
    DateTime? RevokedAtUtc,
    string? CreatedByIp,
    string? UserAgent,
    string? RevocationReason,
    bool IsActive);
