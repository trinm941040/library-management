using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260912010000_SeedSettingsPermissions")]
public partial class SeedSettingsPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                v_admin_role_id uuid;
                v_perm_read_id uuid;
                v_perm_manage_id uuid;
            BEGIN
                SELECT "Id" INTO v_admin_role_id FROM roles WHERE "NormalizedName" = 'ADMINISTRATOR' LIMIT 1;

                -- settings.read
                IF NOT EXISTS (SELECT 1 FROM permissions WHERE "Name" = 'settings.read') THEN
                    v_perm_read_id := gen_random_uuid();
                    INSERT INTO permissions ("Id", "Name", "Description", "Module", "CreatedAtUtc")
                    VALUES (v_perm_read_id, 'settings.read', 'Xem thiết lập hệ thống', 'settings', NOW() AT TIME ZONE 'UTC');
                ELSE
                    SELECT "Id" INTO v_perm_read_id FROM permissions WHERE "Name" = 'settings.read';
                END IF;

                IF v_admin_role_id IS NOT NULL AND v_perm_read_id IS NOT NULL THEN
                    IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE "RoleId" = v_admin_role_id AND "PermissionId" = v_perm_read_id) THEN
                        INSERT INTO role_permissions ("RoleId", "PermissionId")
                        VALUES (v_admin_role_id, v_perm_read_id);
                    END IF;
                END IF;

                -- settings.manage
                IF NOT EXISTS (SELECT 1 FROM permissions WHERE "Name" = 'settings.manage') THEN
                    v_perm_manage_id := gen_random_uuid();
                    INSERT INTO permissions ("Id", "Name", "Description", "Module", "CreatedAtUtc")
                    VALUES (v_perm_manage_id, 'settings.manage', 'Quản lý thiết lập hệ thống', 'settings', NOW() AT TIME ZONE 'UTC');
                ELSE
                    SELECT "Id" INTO v_perm_manage_id FROM permissions WHERE "Name" = 'settings.manage';
                END IF;

                IF v_admin_role_id IS NOT NULL AND v_perm_manage_id IS NOT NULL THEN
                    IF NOT EXISTS (SELECT 1 FROM role_permissions WHERE "RoleId" = v_admin_role_id AND "PermissionId" = v_perm_manage_id) THEN
                        INSERT INTO role_permissions ("RoleId", "PermissionId")
                        VALUES (v_admin_role_id, v_perm_manage_id);
                    END IF;
                END IF;
            END $$;
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                SELECT "Id" FROM permissions WHERE "Name" IN ('settings.read', 'settings.manage')
            );
            DELETE FROM permissions WHERE "Name" IN ('settings.read', 'settings.manage');
        """);
    }
}
