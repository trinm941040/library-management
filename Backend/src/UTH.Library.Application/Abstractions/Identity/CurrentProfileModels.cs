namespace UTH.Library.Application.Abstractions.Identity;

public sealed record EffectiveAuthorizationState(
    Guid UserId,
    string Email,
    bool AccountIsActive,
    bool EmployeeIsActive,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool CanAuthenticate => AccountIsActive && (EmployeeIsActive || Roles.Count > 0);
}

public interface IAuthorizationStateService
{
    Task<EffectiveAuthorizationState?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid userId, Guid familyId, CancellationToken cancellationToken);
}

public sealed record CurrentBranch(Guid Id, string Code, string Name);

public sealed record CurrentProfile(
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
    CurrentBranch? Branch,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid? RowVersion);

public sealed record UpdateCurrentProfileCommand(
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Address,
    Guid RowVersion,
    string? CorrelationId);

public enum CurrentProfileFailure
{
    None,
    NotFound,
    Conflict,
    Validation,
    InvalidPassword
}

public sealed record CurrentProfileResult(
    CurrentProfile? Profile,
    CurrentProfileFailure Failure,
    string? Error)
{
    public bool Succeeded => Failure == CurrentProfileFailure.None;

    public static CurrentProfileResult Success(CurrentProfile? profile = null) =>
        new(profile, CurrentProfileFailure.None, null);

    public static CurrentProfileResult Failed(CurrentProfileFailure failure, string error) =>
        new(null, failure, error);
}

public interface ICurrentProfileService
{
    Task<CurrentProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<CurrentProfileResult> UpdateAsync(Guid userId, UpdateCurrentProfileCommand command, CancellationToken cancellationToken);
    Task<CurrentProfileResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string? ipAddress, string? correlationId, CancellationToken cancellationToken);
}
