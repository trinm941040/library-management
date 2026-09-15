using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260911110000_RepairAuditLogCorrelationId")]
public sealed class RepairAuditLogCorrelationId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE audit_logs
            ADD COLUMN IF NOT EXISTS "CorrelationId" character varying(100);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // CorrelationId belongs to 20260909090000_AddAuditCorrelationId.
        // This repair only restores the column when a later rollback removed it.
    }
}
