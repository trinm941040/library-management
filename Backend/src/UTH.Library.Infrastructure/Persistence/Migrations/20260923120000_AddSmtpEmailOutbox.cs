using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260923120000_AddSmtpEmailOutbox")]
public sealed class AddSmtpEmailOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "AttemptCount", table: "notifications", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "EventCode", table: "notifications", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "IdempotencyKey", table: "notifications", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.Sql("UPDATE notifications SET \"IdempotencyKey\" = gen_random_uuid()::text WHERE \"IdempotencyKey\" IS NULL;");
        migrationBuilder.AlterColumn<string>(name: "IdempotencyKey", table: "notifications", type: "character varying(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200, oldNullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "NextAttemptAtUtc", table: "notifications", type: "timestamp with time zone", nullable: true);
        migrationBuilder.DropIndex(name: "IX_notifications_Status_ScheduledAtUtc", table: "notifications");
        migrationBuilder.CreateIndex(name: "IX_notifications_IdempotencyKey", table: "notifications", column: "IdempotencyKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_notifications_Status_NextAttemptAtUtc", table: "notifications", columns: new[] { "Status", "NextAttemptAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_notifications_IdempotencyKey", table: "notifications");
        migrationBuilder.DropIndex(name: "IX_notifications_Status_NextAttemptAtUtc", table: "notifications");
        migrationBuilder.DropColumn(name: "AttemptCount", table: "notifications");
        migrationBuilder.DropColumn(name: "EventCode", table: "notifications");
        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "notifications");
        migrationBuilder.DropColumn(name: "NextAttemptAtUtc", table: "notifications");
        migrationBuilder.CreateIndex(name: "IX_notifications_Status_ScheduledAtUtc", table: "notifications", columns: new[] { "Status", "ScheduledAtUtc" });
    }
}
