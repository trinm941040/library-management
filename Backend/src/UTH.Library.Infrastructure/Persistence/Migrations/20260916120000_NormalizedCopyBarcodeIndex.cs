using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260916120000_NormalizedCopyBarcodeIndex")]
public sealed class NormalizedCopyBarcodeIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing collisions must be resolved explicitly; this migration never rewrites a barcode.
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM book_copies
                    GROUP BY upper(btrim("Barcode"))
                    HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Duplicate normalized book-copy barcodes exist; resolve them before migration.';
                END IF;
            END $$;
            """);
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX "IX_book_copies_Barcode_Normalized"
            ON book_copies (upper(btrim("Barcode")));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_book_copies_Barcode_Normalized\";");
}
