using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAuthenticationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "IsActive",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_IsActive",
                table: "roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_sessions_FamilyId",
                table: "refresh_token_sessions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_sessions_ParentTokenId",
                table: "refresh_token_sessions",
                column: "ParentTokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roles_IsActive",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_refresh_token_sessions_FamilyId",
                table: "refresh_token_sessions");

            migrationBuilder.DropIndex(
                name: "IX_refresh_token_sessions_ParentTokenId",
                table: "refresh_token_sessions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "roles");
        }
    }
}
