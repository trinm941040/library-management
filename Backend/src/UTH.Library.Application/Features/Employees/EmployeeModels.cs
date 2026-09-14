using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Employees;

public sealed record EmployeeModel(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Address,
    string Position,
    string Department,
    Guid BranchId,
    string BranchCode,
    string BranchName,
    Guid? UserId,
    DateOnly HireDate,
    EmploymentStatus Status,
    Guid ConcurrencyToken,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record EmployeePage(
    IReadOnlyCollection<EmployeeModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record EmployeeListQuery(
    string? Search,
    string? Department,
    string? Position,
    EmploymentStatus? Status,
    Guid? BranchId,
    int PageNumber,
    int PageSize);

public sealed record SaveEmployeeCommand(
    string EmployeeCode,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Address,
    string Position,
    string Department,
    DateOnly HireDate,
    EmploymentStatus Status,
    Guid? BranchId = null,
    Guid? ConcurrencyToken = null,
    bool DeactivateLinkedAccount = false);

public sealed record EmployeeBranchModel(Guid Id, string Code, string Name);

public enum EmployeeManagementFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record EmployeeManagementResult(
    EmployeeModel? Employee,
    EmployeeManagementFailure Failure,
    IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Failure == EmployeeManagementFailure.None;

    public static EmployeeManagementResult Success(EmployeeModel? employee = null) =>
        new(employee, EmployeeManagementFailure.None, []);

    public static EmployeeManagementResult Failed(EmployeeManagementFailure failure, params string[] errors) =>
        new(null, failure, errors);
}
