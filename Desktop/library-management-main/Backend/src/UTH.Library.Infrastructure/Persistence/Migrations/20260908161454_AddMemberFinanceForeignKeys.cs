using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberFinanceForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_fine_adjustments_members_MemberId",
                table: "fine_adjustments",
                column: "MemberId",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fine_payments_members_MemberId",
                table: "fine_payments",
                column: "MemberId",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fine_adjustments_members_MemberId",
                table: "fine_adjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_fine_payments_members_MemberId",
                table: "fine_payments");
        }
    }
}
