using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public sealed class EmployeeTests
{
    [Fact]
    public void Create_ValidInformation_NormalizesValues()
    {
        var now = new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

        var employee = Employee.Create(
            " EMP-001 ",
            " Nguyen Van A ",
            " EMPLOYEE@EXAMPLE.COM ",
            " 0901234567 ",
            new DateOnly(1995, 5, 10),
            " Ho Chi Minh City ",
            " Librarian ",
            " Circulation ",
            new DateOnly(2024, 1, 15),
            EmploymentStatus.Active,
            now);

        Assert.Equal("EMP-001", employee.EmployeeCode);
        Assert.Equal("Nguyen Van A", employee.FullName);
        Assert.Equal("employee@example.com", employee.Email);
        Assert.Equal("0901234567", employee.PhoneNumber);
        Assert.Equal(now, employee.CreatedAtUtc);
        Assert.Equal(now, employee.UpdatedAtUtc);
    }

    [Fact]
    public void Update_DateOfBirthNotBeforeHireDate_ThrowsArgumentException()
    {
        var now = DateTime.UtcNow;
        var employee = Employee.Create(
            "EMP-001",
            "Nguyen Van A",
            "employee@example.com",
            null,
            new DateOnly(1995, 5, 10),
            null,
            "Librarian",
            "Circulation",
            new DateOnly(2024, 1, 15),
            EmploymentStatus.Active,
            now);

        Assert.Throws<ArgumentException>(() => employee.Update(
            "EMP-001",
            "Nguyen Van A",
            "employee@example.com",
            null,
            new DateOnly(2025, 1, 1),
            null,
            "Librarian",
            "Circulation",
            new DateOnly(2024, 1, 15),
            EmploymentStatus.Active,
            now));
    }
}
