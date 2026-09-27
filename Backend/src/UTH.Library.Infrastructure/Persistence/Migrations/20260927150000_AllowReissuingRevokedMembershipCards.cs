using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260927150000_AllowReissuingRevokedMembershipCards")]
public sealed class AllowReissuingRevokedMembershipCards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_membership_cards_MemberId",
            table: "membership_cards");

        migrationBuilder.CreateIndex(
            name: "IX_membership_cards_MemberId",
            table: "membership_cards",
            column: "MemberId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_membership_cards_MemberId",
            table: "membership_cards");

        migrationBuilder.CreateIndex(
            name: "IX_membership_cards_MemberId",
            table: "membership_cards",
            column: "MemberId",
            unique: true);
    }
}
