using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public sealed record EmployeeQuery(
    string? Search,
    string? Department,
    string? Position,
    EmploymentStatus? Status,
    int PageNumber,
    int PageSize);

public interface IEmployeeRepository
{
    Task<(IReadOnlyCollection<Employee> Items, int TotalCount)> GetAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken);

    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> EmployeeCodeExistsAsync(string employeeCode, Guid? excludingId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    void Remove(Employee employee);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
