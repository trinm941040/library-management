using System.Net.Mail;

namespace UTH.Library.Domain.Entities;

public enum EmploymentStatus
{
    Active,
    OnLeave,
    Inactive,
    Terminated
}

public sealed class Employee
{
    private Employee()
    {
        EmployeeCode = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        Position = string.Empty;
        Department = string.Empty;
    }

    public Guid Id { get; private set; }
    public string EmployeeCode { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string Position { get; private set; }
    public string Department { get; private set; }
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public DateOnly HireDate { get; private set; }
    public EmploymentStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Employee Create(
        string employeeCode,
        string fullName,
        string email,
        string? phoneNumber,
        DateOnly? dateOfBirth,
        string? address,
        string position,
        string department,
        DateOnly hireDate,
        EmploymentStatus status,
        DateTime createdAtUtc,
        Guid? branchId = null)
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = createdAtUtc,
            BranchId = branchId ?? Branch.MainBranchId,
            ConcurrencyToken = Guid.NewGuid()
        };
        employee.Update(
            employeeCode,
            fullName,
            email,
            phoneNumber,
            dateOfBirth,
            address,
            position,
            department,
            hireDate,
            status,
            createdAtUtc,
            branchId);
        return employee;
    }

    public void Update(
        string employeeCode,
        string fullName,
        string email,
        string? phoneNumber,
        DateOnly? dateOfBirth,
        string? address,
        string position,
        string department,
        DateOnly hireDate,
        EmploymentStatus status,
        DateTime updatedAtUtc,
        Guid? branchId = null)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), "Employment status is invalid.");
        if (hireDate == default)
            throw new ArgumentException("Hire date is required.", nameof(hireDate));
        if (dateOfBirth is not null && dateOfBirth >= hireDate)
            throw new ArgumentException("Date of birth must be earlier than the hire date.", nameof(dateOfBirth));

        EmployeeCode = Required(employeeCode, nameof(employeeCode), 30).ToUpperInvariant();
        FullName = Required(fullName, nameof(fullName), 150);
        var normalizedEmail = Required(email, nameof(email), 256).ToLowerInvariant();
        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail) || parsedEmail.Address != normalizedEmail)
            throw new ArgumentException("Email address is invalid.", nameof(email));
        Email = normalizedEmail;
        PhoneNumber = Optional(phoneNumber, nameof(phoneNumber), 30);
        DateOfBirth = dateOfBirth;
        Address = Optional(address, nameof(address), 500);
        Position = Required(position, nameof(position), 100);
        Department = Required(department, nameof(department), 100);
        BranchId = branchId ?? BranchId;
        if (BranchId == Guid.Empty)
            BranchId = Branch.MainBranchId;
        HireDate = hireDate;
        Status = status;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void LinkUser(Guid userId, DateTime updatedAtUtc)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User identifier is required.", nameof(userId));
        if (UserId is not null && UserId != userId)
            throw new InvalidOperationException("Employee is already linked to another user account.");

        UserId = userId;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Terminate(DateTime updatedAtUtc)
    {
        ChangeStatus(EmploymentStatus.Terminated, updatedAtUtc);
    }

    public void ChangeStatus(EmploymentStatus status, DateTime updatedAtUtc)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), "Employment status is invalid.");

        Status = status;
        ConcurrencyToken = Guid.NewGuid();
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName)
            : normalized;
    }

    private static string? Optional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName)
            : normalized;
    }
}
