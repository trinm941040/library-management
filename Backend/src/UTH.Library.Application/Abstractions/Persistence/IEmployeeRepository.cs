using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record EmployeeQuery(
    string? Search,
    string? Department,
    string? Position,
    EmploymentStatus? Status,
    Guid? BranchId,
    int PageNumber,
    int PageSize);

public sealed record EmployeeBranch(Guid Id, string Code, string Name, bool IsActive);

public sealed class EmployeeConcurrencyException : Exception
{
    public EmployeeConcurrencyException(Exception innerException)
        : base("Hồ sơ nhân viên đã được cập nhật bởi yêu cầu khác.", innerException)
    {
    }
}

public interface IEmployeeRepository
{
    Task<(IReadOnlyCollection<Employee> Items, int TotalCount)> GetAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> EmployeeCodeExistsAsync(string employeeCode, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken);
    Task<EmployeeBranch?> GetActiveBranchAsync(Guid branchId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EmployeeBranch>> GetBranchesAsync(CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
