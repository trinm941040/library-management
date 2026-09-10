using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260910090000_AddCoreOptimisticConcurrency")]
public sealed class AddCoreOptimisticConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "books", "borrowings", "reservations", "violations" })
            migrationBuilder.AddColumn<Guid>(name: "ConcurrencyToken", table: table, type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "books", "borrowings", "reservations", "violations" })
            migrationBuilder.DropColumn(name: "ConcurrencyToken", table: table);
    }
}
