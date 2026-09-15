using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRolePermissionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000038"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Xem nhật ký hoạt động hệ thống.", "audit-logs", "audit-logs.read" },
                    { new Guid("30000000-0000-0000-0000-000000000039"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Xuất nhật ký hoạt động hệ thống.", "audit-logs", "audit-logs.export" },
                    { new Guid("30000000-0000-0000-0000-000000000040"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Xem cài đặt hệ thống.", "settings", "settings.read" },
                    { new Guid("30000000-0000-0000-0000-000000000041"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Cập nhật cài đặt hệ thống.", "settings", "settings.update" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000038"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000039"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000040"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000041"), new Guid("20000000-0000-0000-0000-000000000001") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM role_permissions
                WHERE "RoleId" = '20000000-0000-0000-0000-000000000001'
                  AND "PermissionId" IN (
                    '30000000-0000-0000-0000-000000000038',
                    '30000000-0000-0000-0000-000000000039',
                    '30000000-0000-0000-0000-000000000040',
                    '30000000-0000-0000-0000-000000000041');

                DELETE FROM permissions
                WHERE "Id" IN (
                    '30000000-0000-0000-0000-000000000038',
                    '30000000-0000-0000-0000-000000000039',
                    '30000000-0000-0000-0000-000000000040',
                    '30000000-0000-0000-0000-000000000041');
                """);
        }
    }
}
