using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Employees;

public sealed class EmployeeService(IEmployeeRepository repository, TimeProvider timeProvider)
{
    public async Task<EmployeePage> GetAsync(EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetAsync(
            new EmployeeQuery(
                query.Search,
                query.Department,
                query.Position,
                query.Status,
                query.PageNumber,
                query.PageSize),
            cancellationToken);

        return new EmployeePage(
            items.Select(Map).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<EmployeeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        return employee is null ? null : Map(employee);
    }

    public async Task<EmployeeManagementResult> CreateAsync(
        SaveEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var duplicate = await ValidateUniquenessAsync(command, null, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var employee = Employee.Create(
                command.EmployeeCode,
                command.FullName,
                command.Email,
                command.PhoneNumber,
                command.DateOfBirth,
                command.Address,
                command.Position,
                command.Department,
                command.HireDate,
                command.Status,
                now);
            await repository.AddAsync(employee, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (ArgumentException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, exception.Message);
        }
    }

    public async Task<EmployeeManagementResult> UpdateAsync(
        Guid id,
        SaveEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Employee was not found.");

        var duplicate = await ValidateUniquenessAsync(command, id, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        try
        {
            employee.Update(
                command.EmployeeCode,
                command.FullName,
                command.Email,
                command.PhoneNumber,
                command.DateOfBirth,
                command.Address,
                command.Position,
                command.Department,
                command.HireDate,
                command.Status,
                timeProvider.GetUtcNow().UtcDateTime);
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (ArgumentException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, exception.Message);
        }
    }

    public async Task<EmployeeManagementResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Employee was not found.");

        repository.Remove(employee);
        await repository.SaveChangesAsync(cancellationToken);
        return EmployeeManagementResult.Success();
    }

    private async Task<EmployeeManagementResult?> ValidateUniquenessAsync(
        SaveEmployeeCommand command,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var employeeCode = command.EmployeeCode.Trim().ToUpperInvariant();
        if (await repository.EmployeeCodeExistsAsync(employeeCode, excludingId, cancellationToken))
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "An employee with this employee code already exists.");

        var email = command.Email.Trim().ToLowerInvariant();
        if (await repository.EmailExistsAsync(email, excludingId, cancellationToken))
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "An employee with this email already exists.");

        return null;
    }

    private static EmployeeModel Map(Employee employee) =>
        new(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.Address,
            employee.Position,
            employee.Department,
            employee.HireDate,
            employee.Status,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);
}
