using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "books",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditionStatement",
                table: "books",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicationYear",
                table: "books",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublisherId",
                table: "books",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "books",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "catalog_migration_issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawValue = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_migration_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_migration_issues_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO authors ("Id", "FullName", "Status")
                SELECT md5('author:' || lower(trim(source."Author")))::uuid,
                    trim(source."Author"),
                    'Active'
                FROM books AS source
                WHERE nullif(trim(source."Author"), '') IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM authors AS existing
                      WHERE lower(trim(existing."FullName")) = lower(trim(source."Author")))
                GROUP BY lower(trim(source."Author")), trim(source."Author")
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO categories ("Id", "Name", "Status")
                SELECT md5('category:' || lower(trim(source."Category")))::uuid,
                    trim(source."Category"),
                    'Active'
                FROM books AS source
                WHERE nullif(trim(source."Category"), '') IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM categories AS existing
                      WHERE lower(trim(existing."Name")) = lower(trim(source."Category")))
                GROUP BY lower(trim(source."Category")), trim(source."Category")
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO book_authors ("BookId", "AuthorId")
                SELECT source."Id", (
                    SELECT author."Id" FROM authors AS author
                    WHERE lower(trim(author."FullName")) = lower(trim(source."Author"))
                    ORDER BY author."Id" LIMIT 1)
                FROM books AS source
                WHERE nullif(trim(source."Author"), '') IS NOT NULL
                ON CONFLICT DO NOTHING;

                INSERT INTO book_categories ("BookId", "CategoryId")
                SELECT source."Id", (
                    SELECT category."Id" FROM categories AS category
                    WHERE lower(trim(category."Name")) = lower(trim(source."Category"))
                    ORDER BY category."Id" LIMIT 1)
                FROM books AS source
                WHERE nullif(trim(source."Category"), '') IS NOT NULL
                ON CONFLICT DO NOTHING;

                INSERT INTO catalog_migration_issues ("Id", "BookId", "Field", "RawValue", "Reason", "CreatedAtUtc")
                SELECT md5(source."Id"::text || ':author')::uuid, source."Id", 'Author', source."Author",
                    'Legacy author value is empty and could not be mapped to an Author.', CURRENT_TIMESTAMP
                FROM books AS source
                WHERE nullif(trim(source."Author"), '') IS NULL
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO catalog_migration_issues ("Id", "BookId", "Field", "RawValue", "Reason", "CreatedAtUtc")
                SELECT md5(source."Id"::text || ':category')::uuid, source."Id", 'Category', source."Category",
                    'Legacy category value is empty and could not be mapped to a Category.', CURRENT_TIMESTAMP
                FROM books AS source
                WHERE nullif(trim(source."Category"), '') IS NULL
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO book_copies ("Id", "BookId", "Barcode", "Condition", "Status", "AcquiredAtUtc")
                SELECT md5(source."Id"::text || ':copy:' || copy_number)::uuid,
                    source."Id",
                    'LEGACY-' || replace(source."Id"::text, '-', '') || '-' || copy_number,
                    'Good', 'Available', source."CreatedAtUtc"
                FROM books AS source
                CROSS JOIN LATERAL generate_series(
                    (SELECT count(*)::integer + 1 FROM book_copies AS existing WHERE existing."BookId" = source."Id"),
                    GREATEST(source."Quantity", 0)) AS copy_number
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_migration_issues_BookId",
                table: "catalog_migration_issues",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_books_PublisherId",
                table: "books",
                column: "PublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_books_Status",
                table: "books",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_books_publishers_PublisherId",
                table: "books",
                column: "PublisherId",
                principalTable: "publishers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_books_publishers_PublisherId",
                table: "books");

            migrationBuilder.DropTable(
                name: "catalog_migration_issues");

            migrationBuilder.DropIndex(
                name: "IX_books_PublisherId",
                table: "books");

            migrationBuilder.DropIndex(
                name: "IX_books_Status",
                table: "books");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "books");

            migrationBuilder.DropColumn(
                name: "EditionStatement",
                table: "books");

            migrationBuilder.DropColumn(
                name: "PublicationYear",
                table: "books");

            migrationBuilder.DropColumn(
                name: "PublisherId",
                table: "books");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "books");
        }
    }
}
