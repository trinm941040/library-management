using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260917120000_AddInventoryReconciliationSnapshot")]
public sealed class AddInventoryReconciliationSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("AreaId", "inventory_audits", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("ShelfId", "inventory_audits", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<bool>("IsExpected", "inventory_audit_items", type: "boolean",
            nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<string>("ExpectedStatus", "inventory_audit_items",
            type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Available");
        migrationBuilder.AddColumn<string>("ExpectedCondition", "inventory_audit_items",
            type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Good");
        migrationBuilder.AddColumn<string>("ActualStatus", "inventory_audit_items",
            type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>("ActualCondition", "inventory_audit_items",
            type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.Sql("""
            UPDATE inventory_audit_items AS item
            SET "ExpectedStatus" = copy."Status", "ExpectedCondition" = copy."Condition"
            FROM book_copies AS copy WHERE item."BookCopyId" = copy."Id";
            """);
        migrationBuilder.CreateIndex("IX_inventory_audits_AreaId_ShelfId_Status", "inventory_audits",
            new[] { "AreaId", "ShelfId", "Status" });
        migrationBuilder.CreateIndex("IX_inventory_audits_ShelfId", "inventory_audits", "ShelfId");
        migrationBuilder.AddForeignKey(name: "FK_inventory_audits_areas_AreaId", table: "inventory_audits",
            column: "AreaId", principalTable: "areas", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_inventory_audits_shelves_ShelfId", table: "inventory_audits",
            column: "ShelfId", principalTable: "shelves", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_inventory_audits_areas_AreaId", "inventory_audits");
        migrationBuilder.DropForeignKey("FK_inventory_audits_shelves_ShelfId", "inventory_audits");
        migrationBuilder.DropIndex("IX_inventory_audits_AreaId_ShelfId_Status", "inventory_audits");
        migrationBuilder.DropIndex("IX_inventory_audits_ShelfId", "inventory_audits");
        migrationBuilder.DropColumn("AreaId", "inventory_audits");
        migrationBuilder.DropColumn("ShelfId", "inventory_audits");
        migrationBuilder.DropColumn("IsExpected", "inventory_audit_items");
        migrationBuilder.DropColumn("ExpectedStatus", "inventory_audit_items");
        migrationBuilder.DropColumn("ExpectedCondition", "inventory_audit_items");
        migrationBuilder.DropColumn("ActualStatus", "inventory_audit_items");
        migrationBuilder.DropColumn("ActualCondition", "inventory_audit_items");
    }
}
