using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "suppliers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "suppliers");

        }
    }
}
