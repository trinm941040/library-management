using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Users;

public sealed class UserFilterRequest
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? Role { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateUserRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(256, MinimumLength = 8)] string Password,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName);

public sealed record UpdateUserRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    bool IsProtected);

public sealed record UserPageResponse(
    IReadOnlyCollection<UserResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
