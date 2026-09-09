using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Profile;

public sealed record BranchResponse(Guid Id, string Code, string Name);
public sealed record CurrentProfileResponse(
    Guid UserId,
    Guid? EmployeeId,
    string DisplayName,
    string LoginIdentifier,
    DateTime? LastLoginAtUtc,
    string? EmployeeCode,
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Address,
    string? Position,
    string? Department,
    string? EmploymentStatus,
    BranchResponse? Branch,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid? RowVersion);

public sealed record UpdateCurrentProfileRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [RegularExpression(@"^\+?[0-9 .()-]{7,30}$", ErrorMessage = "Phone number format is invalid.")] string? PhoneNumber,
    DateOnly? DateOfBirth,
    [StringLength(500)] string? Address,
    Guid RowVersion);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);
