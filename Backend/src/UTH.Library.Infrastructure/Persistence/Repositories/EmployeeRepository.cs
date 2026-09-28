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
        IQueryable<Employee> employees = db.Employees.AsNoTracking().Include(employee => employee.Branch);

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

        if (query.BranchId is not null)
            employees = employees.Where(employee => employee.BranchId == query.BranchId);

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
        db.Employees
            .Include(employee => employee.Branch)
            .SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.UserId == userId, cancellationToken);

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

    public Task<EmployeeBranch?> GetActiveBranchAsync(Guid branchId, CancellationToken cancellationToken) =>
        db.Branches
            .AsNoTracking()
            .Where(branch => branch.Id == branchId && branch.IsActive)
            .Select(branch => new EmployeeBranch(branch.Id, branch.Code, branch.Name, branch.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<EmployeeBranch>> GetBranchesAsync(CancellationToken cancellationToken) =>
        await db.Branches
            .AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Name)
            .Select(branch => new EmployeeBranch(branch.Id, branch.Code, branch.Name, branch.IsActive))
            .ToArrayAsync(cancellationToken);

    public Task AddAsync(Employee employee, CancellationToken cancellationToken) =>
        db.Employees.AddAsync(employee, cancellationToken).AsTask();

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) =>
        db.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new EmployeeConcurrencyException(exception);
        }
    }
}
