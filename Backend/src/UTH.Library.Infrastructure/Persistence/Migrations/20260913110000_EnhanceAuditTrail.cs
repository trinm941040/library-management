using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260913110000_EnhanceAuditTrail")]
public sealed class EnhanceAuditTrail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IpAddress",
            table: "audit_logs",
            type: "character varying(45)",
            maxLength: 45,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RetainUntilUtc",
            table: "audit_logs",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE audit_logs
            SET "RetainUntilUtc" = "CreatedAtUtc" + INTERVAL '365 days'
            WHERE "RetainUntilUtc" IS NULL;
            ALTER TABLE audit_logs ALTER COLUMN "RetainUntilUtc" SET NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_audit_logs_Action_CreatedAtUtc",
            table: "audit_logs",
            columns: ["Action", "CreatedAtUtc"]);
        migrationBuilder.CreateIndex(
            name: "IX_audit_logs_CorrelationId",
            table: "audit_logs",
            column: "CorrelationId");
        migrationBuilder.CreateIndex(
            name: "IX_audit_logs_IpAddress_CreatedAtUtc",
            table: "audit_logs",
            columns: ["IpAddress", "CreatedAtUtc"]);
        migrationBuilder.CreateIndex(
            name: "IX_audit_logs_RetainUntilUtc",
            table: "audit_logs",
            column: "RetainUntilUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_audit_logs_Action_CreatedAtUtc", table: "audit_logs");
        migrationBuilder.DropIndex(name: "IX_audit_logs_CorrelationId", table: "audit_logs");
        migrationBuilder.DropIndex(name: "IX_audit_logs_IpAddress_CreatedAtUtc", table: "audit_logs");
        migrationBuilder.DropIndex(name: "IX_audit_logs_RetainUntilUtc", table: "audit_logs");
        migrationBuilder.DropColumn(name: "IpAddress", table: "audit_logs");
        migrationBuilder.DropColumn(name: "RetainUntilUtc", table: "audit_logs");
    }
}
