using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260913140000_NormalizeLegacySystemSettingTypes")]
public sealed class NormalizeLegacySystemSettingTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE system_settings
            SET "ValueType" = 'String',
                "IsSecret" = TRUE
            WHERE "ValueType" = 'Secret';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally left empty. "Secret" is a legacy, invalid SettingType;
        // restoring it would make both the previous and current applications unable to read the row.
    }
}
