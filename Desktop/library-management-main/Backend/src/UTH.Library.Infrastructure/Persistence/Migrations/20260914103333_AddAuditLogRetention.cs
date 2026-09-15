using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetainUntilUtc",
                table: "audit_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UtcNow.AddYears(1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetainUntilUtc",
                table: "audit_logs");
        }
    }
}
