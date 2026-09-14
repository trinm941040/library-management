namespace UTH.Library.Application.Abstractions.Identity;

public sealed record AccessAccountListQuery(string? Search, bool? IsActive, int PageNumber, int PageSize);

public sealed record AccessAccountEmployee(
    Guid Id, string EmployeeCode, string FullName, string Email, string EmploymentStatus);

public sealed record AccessAccount(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    AccessAccountEmployee Employee,
    bool IsProtected);

public sealed record AccessAccountPage(
    IReadOnlyCollection<AccessAccount> Items, int PageNumber, int PageSize, int TotalCount);

public sealed record EligibleAccessAccountEmployee(
    Guid Id, string EmployeeCode, string FullName, string Email);

public sealed record AccessAccountSession(
    Guid Id,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? UsedAtUtc,
    DateTime? RevokedAtUtc,
    string? CreatedByIp,
    string? UserAgent,
    string? RevocationReason,
    bool IsActive);

public sealed record CreateAccessAccountCommand(
    Guid EmployeeId,
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyCollection<Guid> RoleIds,
    Guid ActorUserId);

public enum AccessAccountFailure
{
    None,
    NotFound,
    Conflict,
    Validation,
    ProtectedResource
}

public sealed record AccessAccountResult(
    AccessAccount? Account,
    AccessAccountFailure Failure,
    IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Failure == AccessAccountFailure.None;
    public static AccessAccountResult Success(AccessAccount? account = null) =>
        new(account, AccessAccountFailure.None, []);
    public static AccessAccountResult Failed(AccessAccountFailure failure, params string[] errors) =>
        new(null, failure, errors);
}

public interface IAccessAccountService
{
    Task<AccessAccountPage> GetAsync(AccessAccountListQuery query, CancellationToken cancellationToken);
    Task<AccessAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EligibleAccessAccountEmployee>> GetEligibleEmployeesAsync(string? search, CancellationToken cancellationToken);
    Task<AccessAccountResult> CreateAsync(CreateAccessAccountCommand command, CancellationToken cancellationToken);
    Task<AccessAccountResult> SetStatusAsync(Guid id, bool isActive, Guid actorUserId, CancellationToken cancellationToken);
    Task<AccessAccountResult> ResetPasswordAsync(Guid id, string temporaryPassword, Guid actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AccessAccountSession>?> GetSessionsAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessAccountResult> RevokeSessionAsync(Guid id, Guid sessionId, Guid actorUserId, CancellationToken cancellationToken);
}
