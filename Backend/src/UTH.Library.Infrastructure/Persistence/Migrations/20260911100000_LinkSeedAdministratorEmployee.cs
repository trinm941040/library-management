using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260911100000_LinkSeedAdministratorEmployee")]
public sealed class LinkSeedAdministratorEmployee : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM employees
                WHERE "UserId" = '10000000-0000-0000-0000-000000000001'
            ) THEN
                RETURN;
            END IF;

            IF EXISTS (
                SELECT 1 FROM employees
                WHERE lower("Email") = 'admin@example.com'
                   OR "EmployeeCode" = 'SYSTEM-ADMIN'
                   OR "Id" = '50000000-0000-0000-0000-000000000001'
            ) THEN
                RAISE EXCEPTION
                    'Cannot create the seeded administrator employee because its email, code, or id conflicts with existing employee data. Map the administrator user explicitly before applying this migration.';
            END IF;

            INSERT INTO employees (
                "Id", "EmployeeCode", "FullName", "Email", "PhoneNumber", "DateOfBirth",
                "Address", "Position", "Department", "BranchId", "UserId", "HireDate",
                "Status", "ConcurrencyToken", "CreatedAtUtc", "UpdatedAtUtc"
            ) VALUES (
                '50000000-0000-0000-0000-000000000001',
                'SYSTEM-ADMIN',
                'Quản trị viên hệ thống',
                'admin@example.com',
                NULL,
                NULL,
                NULL,
                'Quản trị viên hệ thống',
                'Công nghệ thông tin',
                '40000000-0000-0000-0000-000000000001',
                '10000000-0000-0000-0000-000000000001',
                DATE '2026-08-26',
                'Active',
                '50000000-0000-0000-0000-000000000002',
                TIMESTAMPTZ '2026-08-26 00:00:00+00',
                TIMESTAMPTZ '2026-08-26 00:00:00+00'
            );
        END $$;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep employee/history data on rollback. Re-applying Up is idempotent because
        // the administrator remains linked to this employee.
    }
}
