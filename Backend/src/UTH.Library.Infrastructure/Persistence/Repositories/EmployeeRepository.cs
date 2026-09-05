using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository(LibraryDbContext db) : IEmployeeRepository
{
    public async Task<(IReadOnlyCollection<Employee> Items, int TotalCount)> GetAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken)
    {
        var employees = db.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            employees = employees.Where(employee =>
                EF.Functions.ILike(employee.EmployeeCode, search) ||
                EF.Functions.ILike(employee.FullName, search) ||
                EF.Functions.ILike(employee.Email, search) ||
                (employee.PhoneNumber != null && EF.Functions.ILike(employee.PhoneNumber, search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = query.Department.Trim();
            employees = employees.Where(employee => EF.Functions.ILike(employee.Department, department));
        }

        if (!string.IsNullOrWhiteSpace(query.Position))
        {
            var position = query.Position.Trim();
            employees = employees.Where(employee => EF.Functions.ILike(employee.Position, position));
        }

        if (query.Status is not null)
            employees = employees.Where(employee => employee.Status == query.Status);

        var totalCount = await employees.CountAsync(cancellationToken);
        var items = await employees
            .OrderBy(employee => employee.EmployeeCode)
            .ThenBy(employee => employee.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Employees.SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public Task<bool> EmployeeCodeExistsAsync(
        string employeeCode,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        db.Employees.AnyAsync(
            employee => employee.EmployeeCode == employeeCode &&
                        (excludingId == null || employee.Id != excludingId),
            cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        db.Employees.AnyAsync(
            employee => employee.Email == email &&
                        (excludingId == null || employee.Id != excludingId),
            cancellationToken);

    public Task AddAsync(Employee employee, CancellationToken cancellationToken) =>
        db.Employees.AddAsync(employee, cancellationToken).AsTask();

    public void Remove(Employee employee) => db.Employees.Remove(employee);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
