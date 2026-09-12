using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260912020000_FixSystemSettingValueType")]
public partial class FixSystemSettingValueType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Change Value column from jsonb to text for plain string storage
        migrationBuilder.Sql("""
            ALTER TABLE system_settings ALTER COLUMN "Value" TYPE text USING "Value"::text;
            ALTER TABLE configuration_packages ALTER COLUMN "Data" TYPE text USING "Data"::text;
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE system_settings ALTER COLUMN "Value" TYPE jsonb USING "Value"::jsonb;
        """);
    }
}
