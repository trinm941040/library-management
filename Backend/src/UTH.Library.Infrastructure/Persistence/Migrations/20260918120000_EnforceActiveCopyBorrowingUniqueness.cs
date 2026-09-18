using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260918120000_EnforceActiveCopyBorrowingUniqueness")]
public sealed class EnforceActiveCopyBorrowingUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_borrowings_ActiveBookCopyId",
            table: "borrowings",
            column: "BookCopyId",
            unique: true,
            filter: "\"ReturnedAtUtc\" IS NULL AND \"BookCopyId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_borrowings_ActiveBookCopyId",
            table: "borrowings");
    }
}
