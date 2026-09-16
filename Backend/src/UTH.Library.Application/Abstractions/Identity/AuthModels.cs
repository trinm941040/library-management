namespace UTH.Library.Application.Abstractions.Identity;

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string User = "User";
}

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDeactivate = "users.deactivate";
    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssign = "roles.assign";
    public const string PermissionsRead = "permissions.read";
    public const string PermissionsCreate = "permissions.create";
    public const string PermissionsUpdate = "permissions.update";
    public const string PermissionsDelete = "permissions.delete";
    public const string EmployeesRead = "employees.read";
    public const string EmployeesCreate = "employees.create";
    public const string EmployeesUpdate = "employees.update";
    public const string EmployeesDelete = "employees.delete";
    public const string TodosRead = "todos.read";
    public const string TodosCreate = "todos.create";
    public const string TodosUpdate = "todos.update";
    public const string TodosDelete = "todos.delete";
    public const string BooksRead = "books.read";
    public const string BooksCreate = "books.create";
    public const string BooksUpdate = "books.update";
    public const string BooksDelete = "books.delete";
    public const string BorrowingsRead = "borrowings.read";
    public const string BorrowingsCreate = "borrowings.create";
    public const string BorrowingsReturn = "borrowings.return";
    public const string ReservationsRead = "reservations.read";
    public const string ReservationsCreate = "reservations.create";
    public const string ReservationsCancel = "reservations.cancel";
    public const string ReservationsFulfill = "reservations.fulfill";
    public const string ViolationsRead = "violations.read";
    public const string ViolationsCreate = "violations.create";
    public const string ViolationsResolve = "violations.resolve";
    public const string MembersRead = "members.read";
    public const string MembersCreate = "members.create";
    public const string MembersUpdate = "members.update";
    public const string MembersManageCards = "members.manage-cards";
    public const string MembersManageRestrictions = "members.manage-restrictions";
    public const string MembersManageFinances = "members.manage-finances";
    public const string AuditLogsRead = "audit-logs.read";
    public const string AuditLogsExport = "audit-logs.export";
    public const string SettingsRead = "settings.read";
    public const string SettingsUpdate = "settings.update";
    public const string CirculationPoliciesRead = "circulation-policies.read";
    public const string CirculationPoliciesManage = "circulation-policies.manage";
    public const string LocationsRead = "locations.read";
    public const string LocationsCreate = "locations.create";
    public const string LocationsUpdate = "locations.update";
    public const string LocationsDeactivate = "locations.deactivate";
    public const string CopiesRead = "copies.read";
    public const string CopiesCreate = "copies.create";
    public const string CopiesUpdate = "copies.update";

    public static readonly IReadOnlyList<string> All =
    [
        UsersRead, UsersCreate, UsersUpdate, UsersDeactivate,
        RolesRead, RolesCreate, RolesUpdate, RolesDelete, RolesAssign,
        PermissionsRead, PermissionsCreate, PermissionsUpdate, PermissionsDelete,
        EmployeesRead, EmployeesCreate, EmployeesUpdate, EmployeesDelete,
        TodosRead, TodosCreate, TodosUpdate, TodosDelete,
        BooksRead, BooksCreate, BooksUpdate, BooksDelete,
        BorrowingsRead, BorrowingsCreate, BorrowingsReturn,
        ReservationsRead, ReservationsCreate, ReservationsCancel, ReservationsFulfill,
        ViolationsRead, ViolationsCreate, ViolationsResolve,
        MembersRead, MembersCreate, MembersUpdate, MembersManageCards, MembersManageRestrictions, MembersManageFinances,
        AuditLogsRead, AuditLogsExport,
        SettingsRead, SettingsUpdate,
        CirculationPoliciesRead, CirculationPoliciesManage,
        LocationsRead, LocationsCreate, LocationsUpdate, LocationsDeactivate,
        CopiesRead, CopiesCreate, CopiesUpdate
    ];
}

public sealed record AuthResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    CurrentProfile CurrentUser);

public sealed record EffectiveAuthorizationState(
    Guid UserId,
    string Email,
    bool AccountIsActive,
    bool EmployeeIsActive,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool CanAuthenticate => AccountIsActive && EmployeeIsActive;
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
    string? CorrelationId = null);

public enum CurrentProfileFailure { None, NotFound, Validation, Conflict, InvalidPassword }
public sealed record CurrentProfileResult(CurrentProfile? Profile, CurrentProfileFailure Failure, string? Error)
{
    public bool Succeeded => Failure == CurrentProfileFailure.None;
    public static CurrentProfileResult Success(CurrentProfile? profile = null) => new(profile, CurrentProfileFailure.None, null);
    public static CurrentProfileResult Failed(CurrentProfileFailure failure, string error) => new(null, failure, error);
}

public interface ICurrentProfileService
{
    Task<CurrentProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<CurrentProfileResult> UpdateAsync(Guid userId, UpdateCurrentProfileCommand command, CancellationToken cancellationToken);
    Task<CurrentProfileResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string? ipAddress, string? correlationId, CancellationToken cancellationToken);
}

public interface IAuthorizationStateService
{
    Task<EffectiveAuthorizationState?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid userId, Guid familyId, CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<(bool Succeeded, string? Error, AuthResult? Result)> LoginAsync(string email, string password, string? ipAddress, string? userAgent, string? correlationId, CancellationToken cancellationToken);
    Task<(bool Succeeded, string? Error, AuthResult? Result)> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, string? correlationId, CancellationToken cancellationToken);
    Task<bool> LogoutAsync(string refreshToken, string? ipAddress, string? correlationId, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid userId, string? ipAddress, string? correlationId, CancellationToken cancellationToken);
    Task<CurrentProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
}
