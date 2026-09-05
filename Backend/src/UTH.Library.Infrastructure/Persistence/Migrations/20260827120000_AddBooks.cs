using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260827120000_AddBooks")]
public partial class AddBooks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "books",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Isbn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_books", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_books_Isbn",
            table: "books",
            column: "Isbn",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_books_Title",
            table: "books",
            column: "Title");

        migrationBuilder.Sql("""
            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000018', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read books.', 'books', 'books.read'),
                ('30000000-0000-0000-0000-000000000019', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create books.', 'books', 'books.create'),
                ('30000000-0000-0000-0000-000000000020', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Update books.', 'books', 'books.update'),
                ('30000000-0000-0000-0000-000000000021', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Delete books.', 'books', 'books.delete')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('books.read', 'books.create', 'books.update', 'books.delete')
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                '30000000-0000-0000-0000-000000000018',
                '30000000-0000-0000-0000-000000000019',
                '30000000-0000-0000-0000-000000000020',
                '30000000-0000-0000-0000-000000000021');

            DELETE FROM permissions
            WHERE "Name" IN ('books.read', 'books.create', 'books.update', 'books.delete');
            """);

        migrationBuilder.DropTable(name: "books");
    }
}
