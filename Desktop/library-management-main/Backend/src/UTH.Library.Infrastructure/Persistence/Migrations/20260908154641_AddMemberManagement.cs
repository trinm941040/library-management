using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fine_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AdjustedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdjustedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fine_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fine_adjustments_violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fine_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fine_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fine_payments_violations_ViolationId",
                        column: x => x.ViolationId,
                        principalTable: "violations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MemberGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorrowingLimit = table.Column<int>(type: "integer", nullable: false),
                    LoanPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "member_restrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RemovedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_restrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_restrictions_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "membership_cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_membership_cards_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000032"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Read member profiles and history.", "members", "members.read" },
                    { new Guid("30000000-0000-0000-0000-000000000033"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Create member profiles.", "members", "members.create" },
                    { new Guid("30000000-0000-0000-0000-000000000034"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Update member profiles and limits.", "members", "members.update" },
                    { new Guid("30000000-0000-0000-0000-000000000035"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Issue and maintain membership cards.", "members", "members.manage-cards" },
                    { new Guid("30000000-0000-0000-0000-000000000036"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Manage member transaction restrictions.", "members", "members.manage-restrictions" },
                    { new Guid("30000000-0000-0000-0000-000000000037"), new DateTime(2026, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Record fine payments and adjustments.", "members", "members.manage-finances" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000032"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000033"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000034"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000035"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000036"), new Guid("20000000-0000-0000-0000-000000000001") },
                    { new Guid("30000000-0000-0000-0000-000000000037"), new Guid("20000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_fine_adjustments_MemberId_ViolationId",
                table: "fine_adjustments",
                columns: new[] { "MemberId", "ViolationId" });

            migrationBuilder.CreateIndex(
                name: "IX_fine_adjustments_ViolationId",
                table: "fine_adjustments",
                column: "ViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_fine_payments_MemberId_ViolationId",
                table: "fine_payments",
                columns: new[] { "MemberId", "ViolationId" });

            migrationBuilder.CreateIndex(
                name: "IX_fine_payments_ViolationId",
                table: "fine_payments",
                column: "ViolationId");

            migrationBuilder.CreateIndex(
                name: "IX_member_restrictions_MemberId_RemovedAtUtc",
                table: "member_restrictions",
                columns: new[] { "MemberId", "RemovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_members_Email",
                table: "members",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_MemberCode",
                table: "members",
                column: "MemberCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_MemberGroup",
                table: "members",
                column: "MemberGroup");

            migrationBuilder.CreateIndex(
                name: "IX_members_Status",
                table: "members",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_membership_cards_CardNumber",
                table: "membership_cards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membership_cards_MemberId",
                table: "membership_cards",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fine_adjustments");

            migrationBuilder.DropTable(
                name: "fine_payments");

            migrationBuilder.DropTable(
                name: "member_restrictions");

            migrationBuilder.DropTable(
                name: "membership_cards");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000032"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000033"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000034"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000035"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000036"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("30000000-0000-0000-0000-000000000037"), new Guid("20000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000037"));
        }
    }
}
