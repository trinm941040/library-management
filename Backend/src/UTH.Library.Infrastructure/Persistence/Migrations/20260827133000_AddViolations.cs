using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260827133000_AddViolations")]
public partial class AddViolations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "violations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BorrowerId = table.Column<Guid>(type: "uuid", nullable: false),
                BorrowerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                BorrowerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                BookId = table.Column<Guid>(type: "uuid", nullable: true),
                BookTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                FineAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_violations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_violations_RecordedAtUtc",
            table: "violations",
            column: "RecordedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_violations_BorrowerId_ResolvedAtUtc",
            table: "violations",
            columns: new[] { "BorrowerId", "ResolvedAtUtc" });

        migrationBuilder.Sql("""
            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000025', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read violations.', 'violations', 'violations.read'),
                ('30000000-0000-0000-0000-000000000026', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create violations.', 'violations', 'violations.create'),
                ('30000000-0000-0000-0000-000000000027', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Resolve violations.', 'violations', 'violations.resolve')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('violations.read', 'violations.create', 'violations.resolve')
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                '30000000-0000-0000-0000-000000000025',
                '30000000-0000-0000-0000-000000000026',
                '30000000-0000-0000-0000-000000000027');

            DELETE FROM permissions
            WHERE "Name" IN ('violations.read', 'violations.create', 'violations.resolve');
            """);

        migrationBuilder.DropTable(name: "violations");
    }
}
