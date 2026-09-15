using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

public partial class SeedAdministrator : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000001', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read users.', 'users', 'users.read'),
                ('30000000-0000-0000-0000-000000000002', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create users.', 'users', 'users.create'),
                ('30000000-0000-0000-0000-000000000003', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Update users.', 'users', 'users.update'),
                ('30000000-0000-0000-0000-000000000004', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Deactivate users.', 'users', 'users.deactivate'),
                ('30000000-0000-0000-0000-000000000005', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read roles.', 'roles', 'roles.read'),
                ('30000000-0000-0000-0000-000000000006', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create roles.', 'roles', 'roles.create'),
                ('30000000-0000-0000-0000-000000000007', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Update roles.', 'roles', 'roles.update'),
                ('30000000-0000-0000-0000-000000000008', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Assign roles and permissions.', 'roles', 'roles.assign'),
                ('30000000-0000-0000-0000-000000000009', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read permissions.', 'permissions', 'permissions.read'),
                ('30000000-0000-0000-0000-000000000010', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Read todos.', 'todos', 'todos.read'),
                ('30000000-0000-0000-0000-000000000011', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Create todos.', 'todos', 'todos.create'),
                ('30000000-0000-0000-0000-000000000012', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Update todos.', 'todos', 'todos.update'),
                ('30000000-0000-0000-0000-000000000013', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Delete todos.', 'todos', 'todos.delete')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO roles ("Id", "ConcurrencyStamp", "CreatedAtUtc", "Description", "IsSystemRole", "Name", "NormalizedName")
            VALUES ('20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Full system access.', TRUE, 'Administrator', 'ADMINISTRATOR')
            ON CONFLICT ("NormalizedName") DO UPDATE
            SET "Description" = EXCLUDED."Description", "IsSystemRole" = TRUE;

            INSERT INTO users (
                "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAtUtc", "DisplayName", "Email",
                "EmailConfirmed", "IsActive", "LockoutEnabled", "NormalizedEmail", "NormalizedUserName",
                "PasswordHash", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName")
            VALUES (
                '10000000-0000-0000-0000-000000000001', 0, '10000000-0000-0000-0000-000000000003',
                TIMESTAMPTZ '2026-08-26T00:00:00Z', 'System Administrator', 'admin@example.com', TRUE, TRUE,
                TRUE, 'ADMIN@EXAMPLE.COM', 'ADMIN@EXAMPLE.COM',
                '$argon2id$v=19$m=65536,t=3,p=2$gbASegqg+aS71bLWtCPomQ==$3AHGi//TyjMTP6Rj34P/L5yNoeZWZoEHMs2qZ+zMHd0=',
                FALSE, '10000000-0000-0000-0000-000000000002', FALSE, 'admin@example.com')
            ON CONFLICT ("NormalizedUserName") DO UPDATE
            SET "Email" = EXCLUDED."Email",
                "NormalizedEmail" = EXCLUDED."NormalizedEmail",
                "EmailConfirmed" = TRUE,
                "DisplayName" = EXCLUDED."DisplayName",
                "IsActive" = TRUE,
                "PasswordHash" = EXCLUDED."PasswordHash",
                "SecurityStamp" = EXCLUDED."SecurityStamp";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN (
                  'users.read', 'users.create', 'users.update', 'users.deactivate',
                  'roles.read', 'roles.create', 'roles.update', 'roles.assign', 'permissions.read',
                  'todos.read', 'todos.create', 'todos.update', 'todos.delete')
            ON CONFLICT DO NOTHING;

            INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
            SELECT app_user."Id", role."Id"
            FROM users AS app_user
            CROSS JOIN roles AS role
            WHERE app_user."NormalizedUserName" = 'ADMIN@EXAMPLE.COM'
              AND role."NormalizedName" = 'ADMINISTRATOR'
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AspNetUserRoles"
            WHERE "UserId" = '10000000-0000-0000-0000-000000000001'
              AND "RoleId" = '20000000-0000-0000-0000-000000000001';

            DELETE FROM role_permissions
            WHERE "RoleId" = '20000000-0000-0000-0000-000000000001';

            DELETE FROM users WHERE "Id" = '10000000-0000-0000-0000-000000000001';
            DELETE FROM roles WHERE "Id" = '20000000-0000-0000-0000-000000000001';
            DELETE FROM permissions WHERE "Id"::text LIKE '30000000-0000-0000-0000-%';
            """);
    }
}
