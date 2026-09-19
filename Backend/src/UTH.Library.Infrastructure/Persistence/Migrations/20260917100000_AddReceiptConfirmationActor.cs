using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260917100000_AddReceiptConfirmationActor")]
public sealed class AddReceiptConfirmationActor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "CreatedByUserId", table: "discrepancy_reports",
            type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_discrepancy_reports_CreatedByUserId",
            table: "discrepancy_reports", column: "CreatedByUserId");
        migrationBuilder.AddForeignKey(name: "FK_discrepancy_reports_users_CreatedByUserId",
            table: "discrepancy_reports", column: "CreatedByUserId", principalTable: "users",
            principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_discrepancy_reports_users_CreatedByUserId",
            table: "discrepancy_reports");
        migrationBuilder.DropIndex(name: "IX_discrepancy_reports_CreatedByUserId",
            table: "discrepancy_reports");
        migrationBuilder.DropColumn(name: "CreatedByUserId", table: "discrepancy_reports");
    }
}
