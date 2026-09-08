using System.Text.Json;
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
                query.BranchId,
                query.PageNumber,
                query.PageSize),
            cancellationToken);

        return new EmployeePage(
            items.Select(employee => Map(employee)).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyCollection<EmployeeBranchModel>> GetBranchesAsync(CancellationToken cancellationToken) =>
        (await repository.GetBranchesAsync(cancellationToken))
            .Select(branch => new EmployeeBranchModel(branch.Id, branch.Code, branch.Name))
            .ToArray();

    public async Task<EmployeeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        return employee is null ? null : Map(employee);
    }

    public async Task<EmployeeManagementResult> CreateAsync(
        SaveEmployeeCommand command,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        var duplicate = await ValidateUniquenessAsync(command, null, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        var branchId = command.BranchId ?? Branch.MainBranchId;
        var branch = await repository.GetActiveBranchAsync(branchId, cancellationToken);
        if (branch is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Branch is invalid or inactive.");

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
                now,
                branchId);
            await repository.AddAsync(employee, cancellationToken);
            await repository.AddAuditLogAsync(
                AuditLog.Create(actorUserId, "employee.created", nameof(Employee), employee.Id, null, Serialize(employee), now),
                cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee, branch));
        }
        catch (ArgumentException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, exception.Message);
        }
    }

    public async Task<EmployeeManagementResult> UpdateAsync(
        Guid id,
        SaveEmployeeCommand command,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Employee was not found.");

        var duplicate = await ValidateUniquenessAsync(command, id, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        if (command.ConcurrencyToken is not null && employee.ConcurrencyToken != command.ConcurrencyToken)
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "The employee was modified by another request. Reload the employee and try again.");

        var branchId = command.BranchId ?? employee.BranchId;
        if (await repository.GetActiveBranchAsync(branchId, cancellationToken) is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Branch is invalid or inactive.");

        try
        {
            var beforeJson = Serialize(employee);
            var now = timeProvider.GetUtcNow().UtcDateTime;
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
                now,
                branchId);
            await repository.AddAuditLogAsync(
                AuditLog.Create(actorUserId, "employee.updated", nameof(Employee), employee.Id, beforeJson, Serialize(employee), now),
                cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (ArgumentException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, exception.Message);
        }
        catch (EmployeeConcurrencyException)
        {
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "The employee was modified by another request. Reload the employee and try again.");
        }
    }

    public async Task<EmployeeManagementResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Employee was not found.");

        if (employee.Status == EmploymentStatus.Terminated)
            return EmployeeManagementResult.Success(Map(employee));

        var beforeJson = Serialize(employee);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        employee.Terminate(now);
        await repository.AddAuditLogAsync(
            AuditLog.Create(actorUserId, "employee.terminated", nameof(Employee), employee.Id, beforeJson, Serialize(employee), now),
            cancellationToken);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (EmployeeConcurrencyException)
        {
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "The employee was modified by another request. Reload the employee and try again.");
        }
    }

    public async Task<EmployeeManagementResult> UpdateStatusAsync(
        Guid id,
        EmploymentStatus status,
        Guid? concurrencyToken,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        if (!Enum.IsDefined(status))
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Employment status is invalid.");

        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Employee was not found.");

        if (concurrencyToken is not null && employee.ConcurrencyToken != concurrencyToken)
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "The employee was modified by another request. Reload the employee and try again.");

        if (employee.Status == status)
            return EmployeeManagementResult.Success(Map(employee));

        var beforeJson = Serialize(employee);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        employee.ChangeStatus(status, now);
        await repository.AddAuditLogAsync(
            AuditLog.Create(actorUserId, "employee.status-updated", nameof(Employee), employee.Id, beforeJson, Serialize(employee), now),
            cancellationToken);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (EmployeeConcurrencyException)
        {
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "The employee was modified by another request. Reload the employee and try again.");
        }
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

    private static EmployeeModel Map(Employee employee, EmployeeBranch? branch = null) =>
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
            employee.BranchId,
            branch?.Code ?? employee.Branch?.Code ?? string.Empty,
            branch?.Name ?? employee.Branch?.Name ?? string.Empty,
            employee.UserId,
            employee.HireDate,
            employee.Status,
            employee.ConcurrencyToken,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);

    private static string Serialize(Employee employee) => JsonSerializer.Serialize(new
    {
        employee.Id,
        employee.EmployeeCode,
        employee.FullName,
        employee.Email,
        employee.PhoneNumber,
        employee.DateOfBirth,
        employee.Address,
        employee.Position,
        employee.Department,
        employee.BranchId,
        employee.UserId,
        employee.HireDate,
        employee.Status,
        employee.ConcurrencyToken,
        employee.CreatedAtUtc,
        employee.UpdatedAtUtc
    });
}
