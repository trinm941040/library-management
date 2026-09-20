using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteLocationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "branches",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "areas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "branches",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                column: "ConcurrencyToken",
                value: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.CreateIndex(
                name: "IX_shelves_AreaId_Status",
                table: "shelves",
                columns: new[] { "AreaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_areas_BranchId_IsActive",
                table: "areas",
                columns: new[] { "BranchId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shelves_AreaId_Status",
                table: "shelves");

            migrationBuilder.DropIndex(
                name: "IX_areas_BranchId_IsActive",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "areas");
        }
    }
}
