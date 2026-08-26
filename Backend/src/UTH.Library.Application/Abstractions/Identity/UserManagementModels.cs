namespace UTH.Library.Application.Abstractions.Identity;

public sealed record UserListQuery(
    string? Search,
    bool? IsActive,
    string? Role,
    int PageNumber,
    int PageSize);

public sealed record ManagedUser(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles);

public sealed record UserPage(
    IReadOnlyCollection<ManagedUser> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateManagedUserCommand(string Email, string Password, string DisplayName);

public sealed record UpdateManagedUserCommand(string Email, string DisplayName);

public enum UserManagementFailure
{
    None,
    NotFound,
    Conflict,
    Validation,
    SelfDeactivation
}

public sealed record UserManagementResult(
    ManagedUser? User,
    UserManagementFailure Failure,
    IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Failure == UserManagementFailure.None;

    public static UserManagementResult Success(ManagedUser? user = null) =>
        new(user, UserManagementFailure.None, []);

    public static UserManagementResult Failed(UserManagementFailure failure, params string[] errors) =>
        new(null, failure, errors);
}

public interface IUserManagementService
{
    Task<UserPage> GetAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<ManagedUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserManagementResult> CreateAsync(CreateManagedUserCommand command, CancellationToken cancellationToken);
    Task<UserManagementResult> UpdateAsync(Guid id, UpdateManagedUserCommand command, CancellationToken cancellationToken);
    Task<UserManagementResult> DeactivateAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken);
}
