using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Api.Contracts.Employees;

public sealed class EmployeeFilterRequest
{
    public string? Search { get; init; }
    public string? Department { get; init; }
    public string? Position { get; init; }
    public EmploymentStatus? Status { get; init; }
    public Guid? BranchId { get; init; }

    [Range(1, 1_000_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record CreateEmployeeRequest(
    [Required, StringLength(30)] string EmployeeCode,
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [StringLength(30)] string? PhoneNumber,
    DateOnly? DateOfBirth,
    [StringLength(500)] string? Address,
    [Required, StringLength(100)] string Position,
    [Required, StringLength(100)] string Department,
    DateOnly HireDate,
    EmploymentStatus Status,
    Guid? BranchId = null);

public sealed record UpdateEmployeeRequest(
    [Required, StringLength(30)] string EmployeeCode,
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [StringLength(30)] string? PhoneNumber,
    DateOnly? DateOfBirth,
    [StringLength(500)] string? Address,
    [Required, StringLength(100)] string Position,
    [Required, StringLength(100)] string Department,
    DateOnly HireDate,
    EmploymentStatus Status,
    Guid? BranchId = null,
    Guid? ConcurrencyToken = null);

public sealed record UpdateEmployeeStatusRequest(
    EmploymentStatus Status,
    Guid? ConcurrencyToken = null);

public sealed record EmployeeResponse(
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

public sealed record EmployeePageResponse(
    IReadOnlyCollection<EmployeeResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record EmployeeBranchResponse(Guid Id, string Code, string Name);
