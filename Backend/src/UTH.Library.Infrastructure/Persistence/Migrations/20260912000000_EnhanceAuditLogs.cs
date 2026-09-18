using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260912000000_EnhanceAuditLogs")]
public partial class EnhanceAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE audit_logs ADD COLUMN IF NOT EXISTS "CorrelationId" character varying(100);
            ALTER TABLE audit_logs ADD COLUMN IF NOT EXISTS "IpAddress" character varying(64);

            CREATE INDEX IF NOT EXISTS "IX_audit_logs_CorrelationId" ON audit_logs ("CorrelationId");
            CREATE INDEX IF NOT EXISTS "IX_audit_logs_CreatedAtUtc" ON audit_logs ("CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_audit_logs_Action" ON audit_logs ("Action");

            DO $$
            DECLARE
                v_admin_role_id uuid;
                v_perm_id uuid;
            BEGIN
                SELECT "Id" INTO v_admin_role_id FROM roles WHERE "NormalizedName" = 'ADMINISTRATOR' LIMIT 1;

                IF NOT EXISTS (SELECT 1 FROM permissions WHERE "Name" = 'audit-logs.read') THEN
                    v_perm_id := gen_random_uuid();
                    INSERT INTO permissions ("Id", "Name", "Module", "CreatedAtUtc")
                    VALUES (v_perm_id, 'audit-logs.read', 'audit-logs', NOW() AT TIME ZONE 'UTC');
                ELSE
                    SELECT "Id" INTO v_perm_id FROM permissions WHERE "Name" = 'audit-logs.read';
                END IF;

                IF v_admin_role_id IS NOT NULL AND v_perm_id IS NOT NULL THEN
                    IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE "RoleId" = v_admin_role_id AND "PermissionId" = v_perm_id) THEN
                        INSERT INTO role_permissions ("RoleId", "PermissionId")
                        VALUES (v_admin_role_id, v_perm_id);
                    END IF;
                END IF;
            END $$;
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_audit_logs_CorrelationId";
            DROP INDEX IF EXISTS "IX_audit_logs_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_audit_logs_Action";
            ALTER TABLE audit_logs DROP COLUMN IF EXISTS "CorrelationId";
            ALTER TABLE audit_logs DROP COLUMN IF EXISTS "IpAddress";
        """);
    }
}
