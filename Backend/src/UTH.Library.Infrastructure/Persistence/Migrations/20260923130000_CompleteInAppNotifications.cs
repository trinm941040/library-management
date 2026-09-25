using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260923130000_CompleteInAppNotifications")]
public sealed class CompleteInAppNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "CreatedAtUtc", table: "notifications", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.AddColumn<string>(name: "DeepLink", table: "notifications", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MetadataJson", table: "notifications", type: "jsonb", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Severity", table: "notifications", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Info");
        migrationBuilder.Sql("UPDATE notifications SET \"CreatedAtUtc\" = COALESCE(\"SentAtUtc\", \"ScheduledAtUtc\", CURRENT_TIMESTAMP);");
        migrationBuilder.AlterColumn<DateTime>(name: "CreatedAtUtc", table: "notifications", type: "timestamp with time zone", nullable: false, oldClrType: typeof(DateTime), oldType: "timestamp with time zone", oldDefaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.CreateIndex(name: "IX_notifications_RecipientId_ReadAtUtc_CreatedAtUtc", table: "notifications", columns: new[] { "RecipientId", "ReadAtUtc", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_notifications_RecipientId_ReadAtUtc_CreatedAtUtc", table: "notifications");
        migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "notifications");
        migrationBuilder.DropColumn(name: "DeepLink", table: "notifications");
        migrationBuilder.DropColumn(name: "MetadataJson", table: "notifications");
        migrationBuilder.DropColumn(name: "Severity", table: "notifications");
    }
}
