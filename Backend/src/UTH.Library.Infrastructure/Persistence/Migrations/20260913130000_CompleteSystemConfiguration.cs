using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260913130000_CompleteSystemConfiguration")]
public sealed class CompleteSystemConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "ConcurrencyToken",
            table: "system_settings",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AddColumn<bool>(
            name: "IsSecret",
            table: "system_settings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
        migrationBuilder.AddColumn<string>(
            name: "Scope",
            table: "system_settings",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "System");
        migrationBuilder.Sql("""
            UPDATE system_settings SET "Scope" = 'Notifications'
            WHERE "Key" LIKE 'notifications.%';
            UPDATE system_settings SET "Scope" = 'Operations'
            WHERE "Key" LIKE 'operations.%';
            UPDATE system_settings SET "IsSecret" = TRUE
            WHERE "Key" IN ('notifications.smtp.username', 'notifications.smtp.password');

            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM configuration_packages WHERE length("Checksum") > 64) THEN
                    RAISE EXCEPTION 'Configuration package checksum longer than SHA-256 format; normalize it before migration.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropIndex(name: "IX_configuration_packages_Checksum", table: "configuration_packages");
        migrationBuilder.AlterColumn<string>(
            name: "Checksum",
            table: "configuration_packages",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldMaxLength: 128);
        migrationBuilder.CreateIndex(
            name: "IX_configuration_packages_Checksum",
            table: "configuration_packages",
            column: "Checksum");
        migrationBuilder.CreateIndex(
            name: "IX_configuration_packages_CreatedAtUtc",
            table: "configuration_packages",
            column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(
            name: "IX_system_settings_Scope_Key",
            table: "system_settings",
            columns: ["Scope", "Key"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS (
                    SELECT 1 FROM configuration_packages
                    GROUP BY "Checksum" HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Duplicate configuration package checksums prevent restoring the unique index.';
                END IF;
            END $$;
            """);
        migrationBuilder.DropIndex(name: "IX_configuration_packages_Checksum", table: "configuration_packages");
        migrationBuilder.DropIndex(name: "IX_configuration_packages_CreatedAtUtc", table: "configuration_packages");
        migrationBuilder.DropIndex(name: "IX_system_settings_Scope_Key", table: "system_settings");
        migrationBuilder.AlterColumn<string>(
            name: "Checksum",
            table: "configuration_packages",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);
        migrationBuilder.CreateIndex(
            name: "IX_configuration_packages_Checksum",
            table: "configuration_packages",
            column: "Checksum",
            unique: true);
        migrationBuilder.DropColumn(name: "IsSecret", table: "system_settings");
        migrationBuilder.DropColumn(name: "Scope", table: "system_settings");
        migrationBuilder.AlterColumn<Guid>(
            name: "ConcurrencyToken",
            table: "system_settings",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");
    }
}
