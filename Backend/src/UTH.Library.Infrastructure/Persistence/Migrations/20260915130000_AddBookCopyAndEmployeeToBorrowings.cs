using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(LibraryDbContext))]
    [Migration("20260915130000_AddBookCopyAndEmployeeToBorrowings")]
    public partial class AddBookCopyAndEmployeeToBorrowings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookCopyId",
                table: "borrowings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByEmployeeId",
                table: "borrowings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_borrowings_BookCopyId_ReturnedAtUtc",
                table: "borrowings",
                columns: new[] { "BookCopyId", "ReturnedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_borrowings_ProcessedByEmployeeId",
                table: "borrowings",
                column: "ProcessedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_borrowings_book_copies_BookCopyId",
                table: "borrowings",
                column: "BookCopyId",
                principalTable: "book_copies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_borrowings_employees_ProcessedByEmployeeId",
                table: "borrowings",
                column: "ProcessedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_borrowings_book_copies_BookCopyId",
                table: "borrowings");

            migrationBuilder.DropForeignKey(
                name: "FK_borrowings_employees_ProcessedByEmployeeId",
                table: "borrowings");

            migrationBuilder.DropIndex(
                name: "IX_borrowings_BookCopyId_ReturnedAtUtc",
                table: "borrowings");

            migrationBuilder.DropIndex(
                name: "IX_borrowings_ProcessedByEmployeeId",
                table: "borrowings");

            migrationBuilder.DropColumn(
                name: "BookCopyId",
                table: "borrowings");

            migrationBuilder.DropColumn(
                name: "ProcessedByEmployeeId",
                table: "borrowings");
        }
    }
}
