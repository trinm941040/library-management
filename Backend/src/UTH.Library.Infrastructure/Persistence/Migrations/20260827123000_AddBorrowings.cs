using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260827123000_AddBorrowings")]
public partial class AddBorrowings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "borrowings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookId = table.Column<Guid>(type: "uuid", nullable: false),
                BorrowerId = table.Column<Guid>(type: "uuid", nullable: false),
                BorrowerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                BorrowerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                BorrowedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReturnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_borrowings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_borrowings_DueAtUtc",
            table: "borrowings",
            column: "DueAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_borrowings_BookId_BorrowerId_ReturnedAtUtc",
            table: "borrowings",
            columns: new[] { "BookId", "BorrowerId", "ReturnedAtUtc" });

        migrationBuilder.Sql("""
            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000022', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read borrowings.', 'borrowings', 'borrowings.read'),
                ('30000000-0000-0000-0000-000000000023', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create borrowings.', 'borrowings', 'borrowings.create'),
                ('30000000-0000-0000-0000-000000000024', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Return borrowings.', 'borrowings', 'borrowings.return')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('borrowings.read', 'borrowings.create', 'borrowings.return')
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                '30000000-0000-0000-0000-000000000022',
                '30000000-0000-0000-0000-000000000023',
                '30000000-0000-0000-0000-000000000024');

            DELETE FROM permissions
            WHERE "Name" IN ('borrowings.read', 'borrowings.create', 'borrowings.return');
            """);

        migrationBuilder.DropTable(name: "borrowings");
    }
}
