using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompletePhase2DataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_areas_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Biography = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "circulation_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circulation_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "configuration_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuration_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuration_packages_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_audits_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_audits_users_StartedByUserId",
                        column: x => x.StartedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyTemplate = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "publishers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "renewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousDueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NewDueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RenewedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_renewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_renewals_borrowings_BorrowingId",
                        column: x => x.BorrowingId,
                        principalTable: "borrowings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_renewals_users_RenewedByUserId",
                        column: x => x.RenewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Definition = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reports_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_filters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Criteria = table.Column<string>(type: "jsonb", nullable: false),
                    Sort = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_filters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_filters_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "jsonb", nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_system_settings_users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shelves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shelves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shelves_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "book_authors",
                columns: table => new
                {
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_authors", x => new { x.BookId, x.AuthorId });
                    table.ForeignKey(
                        name: "FK_book_authors_authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_authors_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_categories",
                columns: table => new
                {
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_categories", x => new { x.BookId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_book_categories_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_book_categories_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "borrowing_limit_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CirculationPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxActiveLoans = table.Column<int>(type: "integer", nullable: false),
                    LoanDays = table.Column<int>(type: "integer", nullable: false),
                    MaxRenewals = table.Column<int>(type: "integer", nullable: false),
                    ReservationHoldDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_borrowing_limit_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_borrowing_limit_policies_circulation_policies_CirculationPo~",
                        column: x => x.CirculationPolicyId,
                        principalTable: "circulation_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fine_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CirculationPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViolationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AmountPerDay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FixedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fine_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fine_policies_circulation_policies_CirculationPolicyId",
                        column: x => x.CirculationPolicyId,
                        principalTable: "circulation_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_notification_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "notification_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_receipts_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipts_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipts_users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discrepancy_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ActualQuantity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discrepancy_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discrepancy_reports_stock_receipts_StockReceiptId",
                        column: x => x.StockReceiptId,
                        principalTable: "stock_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_receipt_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    DamagedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_receipt_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_receipt_items_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipt_items_stock_receipts_StockReceiptId",
                        column: x => x.StockReceiptId,
                        principalTable: "stock_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_copies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockReceiptItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_copies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_copies_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_copies_shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_book_copies_stock_receipt_items_StockReceiptItemId",
                        column: x => x.StockReceiptItemId,
                        principalTable: "stock_receipt_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inventory_audit_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookCopyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_audit_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_audit_items_book_copies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "book_copies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_audit_items_inventory_audits_InventoryAuditId",
                        column: x => x.InventoryAuditId,
                        principalTable: "inventory_audits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_audit_items_shelves_ActualShelfId",
                        column: x => x.ActualShelfId,
                        principalTable: "shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_audit_items_shelves_ExpectedShelfId",
                        column: x => x.ExpectedShelfId,
                        principalTable: "shelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_areas_BranchId_Code",
                table: "areas",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_authors_FullName",
                table: "authors",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_book_authors_AuthorId",
                table: "book_authors",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_book_categories_CategoryId",
                table: "book_categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_Barcode",
                table: "book_copies",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_BookId_Status",
                table: "book_copies",
                columns: new[] { "BookId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_ShelfId",
                table: "book_copies",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_book_copies_StockReceiptItemId",
                table: "book_copies",
                column: "StockReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_borrowing_limit_policies_CirculationPolicyId_MemberType",
                table: "borrowing_limit_policies",
                columns: new[] { "CirculationPolicyId", "MemberType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_circulation_policies_IsActive",
                table: "circulation_policies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_configuration_packages_Checksum",
                table: "configuration_packages",
                column: "Checksum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuration_packages_CreatedByUserId",
                table: "configuration_packages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_discrepancy_reports_StockReceiptId",
                table: "discrepancy_reports",
                column: "StockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_fine_policies_CirculationPolicyId_ViolationType",
                table: "fine_policies",
                columns: new[] { "CirculationPolicyId", "ViolationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audit_items_ActualShelfId",
                table: "inventory_audit_items",
                column: "ActualShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audit_items_BookCopyId",
                table: "inventory_audit_items",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audit_items_ExpectedShelfId",
                table: "inventory_audit_items",
                column: "ExpectedShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audit_items_InventoryAuditId_BookCopyId",
                table: "inventory_audit_items",
                columns: new[] { "InventoryAuditId", "BookCopyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audits_BranchId_Status",
                table: "inventory_audits",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_audits_StartedByUserId",
                table: "inventory_audits",
                column: "StartedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_Code_Channel",
                table: "notification_templates",
                columns: new[] { "Code", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_Status_ScheduledAtUtc",
                table: "notifications",
                columns: new[] { "Status", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_TemplateId",
                table: "notifications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_publishers_Name",
                table: "publishers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_renewals_BorrowingId_RenewedAtUtc",
                table: "renewals",
                columns: new[] { "BorrowingId", "RenewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_renewals_RenewedByUserId",
                table: "renewals",
                column: "RenewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_reports_Code",
                table: "reports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_CreatedByUserId",
                table: "reports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_filters_OwnerUserId_Scope_Name",
                table: "saved_filters",
                columns: new[] { "OwnerUserId", "Scope", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shelves_AreaId_Code",
                table: "shelves",
                columns: new[] { "AreaId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipt_items_BookId",
                table: "stock_receipt_items",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipt_items_StockReceiptId_BookId",
                table: "stock_receipt_items",
                columns: new[] { "StockReceiptId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipts_BranchId",
                table: "stock_receipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipts_ReceiptNumber",
                table: "stock_receipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipts_ReceivedByUserId",
                table: "stock_receipts",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_receipts_SupplierId",
                table: "stock_receipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Code",
                table: "suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_settings_Key",
                table: "system_settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_settings_UpdatedByUserId",
                table: "system_settings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_authors");

            migrationBuilder.DropTable(
                name: "book_categories");

            migrationBuilder.DropTable(
                name: "borrowing_limit_policies");

            migrationBuilder.DropTable(
                name: "configuration_packages");

            migrationBuilder.DropTable(
                name: "discrepancy_reports");

            migrationBuilder.DropTable(
                name: "fine_policies");

            migrationBuilder.DropTable(
                name: "inventory_audit_items");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "publishers");

            migrationBuilder.DropTable(
                name: "renewals");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "saved_filters");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "circulation_policies");

            migrationBuilder.DropTable(
                name: "book_copies");

            migrationBuilder.DropTable(
                name: "inventory_audits");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "shelves");

            migrationBuilder.DropTable(
                name: "stock_receipt_items");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "stock_receipts");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
