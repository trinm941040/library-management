using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LibraryDbContext))]
    [Migration("20260916230000_AddNotificationReadAtAndAllowedVariables")]
    public partial class AddNotificationReadAtAndAllowedVariables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedVariables",
                table: "notification_templates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_RecipientId_ReadAtUtc",
                table: "notifications",
                columns: new[] { "RecipientId", "ReadAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_RecipientType_RecipientId",
                table: "notifications",
                columns: new[] { "RecipientType", "RecipientId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_RecipientId_ReadAtUtc",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_RecipientType_RecipientId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "AllowedVariables",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                table: "notifications");
        }
    }
}
