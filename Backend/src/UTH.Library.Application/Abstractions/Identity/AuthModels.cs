namespace UTH.Library.Application.Abstractions.Identity;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDeactivate = "users.deactivate";
    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesAssign = "roles.assign";
    public const string PermissionsRead = "permissions.read";
    public const string TodosRead = "todos.read";
    public const string TodosCreate = "todos.create";
    public const string TodosUpdate = "todos.update";
    public const string TodosDelete = "todos.delete";

    public static readonly IReadOnlyList<string> All =
    [
        UsersRead, UsersCreate, UsersUpdate, UsersDeactivate,
        RolesRead, RolesCreate, RolesUpdate, RolesAssign,
        PermissionsRead, TodosRead, TodosCreate, TodosUpdate, TodosDelete
    ];
}

public sealed record AuthResult(Guid UserId, string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc, DateTimeOffset RefreshTokenExpiresAtUtc);
public sealed record UserProfile(Guid Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);

public interface IAuthService
{
    Task<(bool Succeeded, string? Error, AuthResult? Result)> RegisterAsync(string email, string password, string displayName, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? Error, AuthResult? Result)> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? Error, AuthResult? Result)> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<bool> LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken);
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
}