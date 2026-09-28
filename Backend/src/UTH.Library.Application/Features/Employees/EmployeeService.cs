using System.Text.Json;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Employees;

public sealed class EmployeeService(
    IEmployeeRepository repository,
    IUnitOfWork unitOfWork,
    IEmployeeAccountLifecycle accountLifecycle,
    TimeProvider timeProvider)
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
        var validation = Validate(command);
        if (validation is not null) return validation;
        var duplicate = await ValidateUniquenessAsync(command, null, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        var branchId = command.BranchId ?? Branch.MainBranchId;
        var branch = await repository.GetActiveBranchAsync(branchId, cancellationToken);
        if (branch is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Chi nhánh không hợp lệ hoặc đã ngừng hoạt động.");

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
        var validation = Validate(command);
        if (validation is not null) return validation;
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Không tìm thấy hồ sơ nhân viên.");

        var duplicate = await ValidateUniquenessAsync(command, id, cancellationToken);
        if (duplicate is not null)
            return duplicate;

        if (command.ConcurrencyToken is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Thiếu phiên bản dữ liệu nhân viên.");
        if (employee.ConcurrencyToken != command.ConcurrencyToken)
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "Hồ sơ đã được thay đổi bởi yêu cầu khác. Vui lòng tải lại.");

        var branchId = command.BranchId ?? employee.BranchId;
        var branch = await repository.GetActiveBranchAsync(branchId, cancellationToken);
        if (branch is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Chi nhánh không hợp lệ hoặc đã ngừng hoạt động.");

        try
        {
            var beforeJson = Serialize(employee);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await unitOfWork.ExecuteAsync(async ct =>
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
                    now,
                    branchId);
                await DeactivateLinkedAccountIfRequestedAsync(employee, command.DeactivateLinkedAccount, actorUserId, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(
                    actorUserId, "employee.updated", nameof(Employee), employee.Id,
                    beforeJson, Serialize(employee), now));
                return true;
            }, cancellationToken);
            return EmployeeManagementResult.Success(Map(employee, branch));
        }
        catch (ArgumentException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, exception.Message);
        }
        catch (Exception exception) when (exception is EmployeeConcurrencyException or OptimisticConcurrencyException)
        {
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "Hồ sơ đã được thay đổi bởi yêu cầu khác. Vui lòng tải lại.");
        }
        catch (ResourceConflictException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Conflict, exception.Message);
        }
    }

    public async Task<EmployeeManagementResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Không tìm thấy hồ sơ nhân viên.");

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
                "Hồ sơ nhân viên đã được cập nhật bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
        }
    }

    public async Task<EmployeeManagementResult> UpdateStatusAsync(
        Guid id,
        EmploymentStatus status,
        Guid? concurrencyToken,
        bool deactivateLinkedAccount,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        if (!Enum.IsDefined(status))
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Trạng thái làm việc không hợp lệ.");

        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.NotFound, "Không tìm thấy hồ sơ nhân viên.");

        if (concurrencyToken is null)
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Thiếu phiên bản dữ liệu nhân viên.");
        if (employee.ConcurrencyToken != concurrencyToken)
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "Hồ sơ đã được thay đổi bởi yêu cầu khác. Vui lòng tải lại.");

        if (employee.Status == status)
            return EmployeeManagementResult.Success(Map(employee));

        var beforeJson = Serialize(employee);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await unitOfWork.ExecuteAsync(async ct =>
            {
                employee.ChangeStatus(status, now);
                await DeactivateLinkedAccountIfRequestedAsync(employee, deactivateLinkedAccount, actorUserId, ct);
                unitOfWork.AddAuditLog(AuditLog.Create(
                    actorUserId, "employee.status-updated", nameof(Employee), employee.Id,
                    beforeJson, Serialize(employee), now));
                return true;
            }, cancellationToken);
            return EmployeeManagementResult.Success(Map(employee));
        }
        catch (Exception exception) when (exception is EmployeeConcurrencyException or OptimisticConcurrencyException)
        {
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "Hồ sơ đã được thay đổi bởi yêu cầu khác. Vui lòng tải lại.");
        }
        catch (ResourceConflictException exception)
        {
            return EmployeeManagementResult.Failed(EmployeeManagementFailure.Conflict, exception.Message);
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
                "Mã nhân viên đã tồn tại.");

        var email = command.Email.Trim().ToLowerInvariant();
        if (await repository.EmailExistsAsync(email, excludingId, cancellationToken))
            return EmployeeManagementResult.Failed(
                EmployeeManagementFailure.Conflict,
                "Email nhân viên đã tồn tại.");

        return null;
    }

    private static EmployeeManagementResult? Validate(SaveEmployeeCommand command) =>
        new[] { command.EmployeeCode, command.FullName, command.Email, command.Position, command.Department }.Any(string.IsNullOrWhiteSpace)
            ? EmployeeManagementResult.Failed(EmployeeManagementFailure.Validation, "Các trường bắt buộc không được chỉ chứa khoảng trắng.")
            : null;

    private async Task DeactivateLinkedAccountIfRequestedAsync(
        Employee employee,
        bool requested,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!requested || employee.Status != EmploymentStatus.Terminated) return;
        var result = await accountLifecycle.DeactivateAsync(employee.UserId, actorUserId, cancellationToken);
        if (result == LinkedAccountDeactivationResult.Protected)
            throw new ResourceConflictException(
                "Không thể vô hiệu hóa tài khoản quản trị liên kết. Hồ sơ chưa được cập nhật.");
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
