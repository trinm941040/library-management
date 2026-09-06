using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260827130000_AddReservations")]
public partial class AddReservations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BookId = table.Column<Guid>(type: "uuid", nullable: false),
                ReserverId = table.Column<Guid>(type: "uuid", nullable: false),
                ReserverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ReserverEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ReservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FulfilledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reservations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_reservations_ExpiresAtUtc",
            table: "reservations",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_reservations_BookId_ReserverId_FulfilledAtUtc_CancelledAtUtc",
            table: "reservations",
            columns: new[] { "BookId", "ReserverId", "FulfilledAtUtc", "CancelledAtUtc" });

        migrationBuilder.Sql("""
            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000025', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read reservations.', 'reservations', 'reservations.read'),
                ('30000000-0000-0000-0000-000000000026', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create reservations.', 'reservations', 'reservations.create'),
                ('30000000-0000-0000-0000-000000000027', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Cancel reservations.', 'reservations', 'reservations.cancel'),
                ('30000000-0000-0000-0000-000000000028', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Fulfill reservations.', 'reservations', 'reservations.fulfill')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('reservations.read', 'reservations.create', 'reservations.cancel', 'reservations.fulfill')
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
                '30000000-0000-0000-0000-000000000027',
                '30000000-0000-0000-0000-000000000028');

            DELETE FROM permissions
            WHERE "Name" IN ('reservations.read', 'reservations.create', 'reservations.cancel', 'reservations.fulfill');
            """);

        migrationBuilder.DropTable(name: "reservations");
    }
}
