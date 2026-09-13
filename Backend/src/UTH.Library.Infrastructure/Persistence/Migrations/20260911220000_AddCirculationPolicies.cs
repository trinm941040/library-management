using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260911220000_AddCirculationPolicies")]
public partial class AddCirculationPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE circulation_policies ALTER COLUMN "Name" TYPE character varying(200);

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveFromUtc'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveFrom'
                ) THEN
                    ALTER TABLE circulation_policies RENAME COLUMN "EffectiveFromUtc" TO "EffectiveFrom";
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveToUtc'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveTo'
                ) THEN
                    ALTER TABLE circulation_policies RENAME COLUMN "EffectiveToUtc" TO "EffectiveTo";
                END IF;
            END $$;

            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Description" character varying(1000);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Version" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MemberGroup" character varying(100);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "DocumentType" character varying(100);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "BranchId" uuid;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "EffectiveFrom" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "EffectiveTo" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxLoanBooks" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "LoanPeriodDays" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxRenewals" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "RenewalPeriodDays" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "HoldDays" integer;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "BlockIfOverdue" boolean;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "FinePerDay" numeric(18,2);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "FixedFineAmount" numeric(18,2);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxFineAmount" numeric(18,2);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "LostBookPenaltyRatio" numeric(18,2);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "UpdatedAtUtc" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveFromUtc'
                ) THEN
                    EXECUTE 'UPDATE circulation_policies SET "EffectiveFrom" = COALESCE("EffectiveFrom", "EffectiveFromUtc", NOW())';
                    ALTER TABLE circulation_policies ALTER COLUMN "EffectiveFromUtc" DROP NOT NULL;
                END IF;
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveToUtc'
                ) THEN
                    EXECUTE 'UPDATE circulation_policies SET "EffectiveTo" = COALESCE("EffectiveTo", "EffectiveToUtc")';
                END IF;
            END $$;

            UPDATE circulation_policies
            SET "Version" = COALESCE("Version", 1),
                "EffectiveFrom" = COALESCE("EffectiveFrom", NOW()),
                "MaxLoanBooks" = COALESCE("MaxLoanBooks", 5),
                "LoanPeriodDays" = COALESCE("LoanPeriodDays", 14),
                "MaxRenewals" = COALESCE("MaxRenewals", 2),
                "RenewalPeriodDays" = COALESCE("RenewalPeriodDays", 7),
                "HoldDays" = COALESCE("HoldDays", 3),
                "BlockIfOverdue" = COALESCE("BlockIfOverdue", TRUE),
                "FinePerDay" = COALESCE("FinePerDay", 5000),
                "FixedFineAmount" = COALESCE("FixedFineAmount", 0),
                "MaxFineAmount" = COALESCE("MaxFineAmount", 100000),
                "LostBookPenaltyRatio" = COALESCE("LostBookPenaltyRatio", 150),
                "CreatedAtUtc" = COALESCE("CreatedAtUtc", "EffectiveFrom", NOW());

            ALTER TABLE circulation_policies ALTER COLUMN "Version" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "EffectiveFrom" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "MaxLoanBooks" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "LoanPeriodDays" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "MaxRenewals" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "RenewalPeriodDays" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "HoldDays" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "BlockIfOverdue" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "FinePerDay" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "FixedFineAmount" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "MaxFineAmount" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "LostBookPenaltyRatio" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "CreatedAtUtc" SET NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_circulation_policies_IsActive_EffectiveFrom_EffectiveTo"
                ON circulation_policies ("IsActive", "EffectiveFrom", "EffectiveTo");
            CREATE INDEX IF NOT EXISTS "IX_circulation_policies_MemberGroup_DocumentType_BranchId"
                ON circulation_policies ("MemberGroup", "DocumentType", "BranchId");
            CREATE INDEX IF NOT EXISTS "IX_circulation_policies_Name_Version"
                ON circulation_policies ("Name", "Version");

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'FK_circulation_policies_branches_BranchId'
                ) THEN
                    ALTER TABLE circulation_policies
                    ADD CONSTRAINT "FK_circulation_policies_branches_BranchId"
                    FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE RESTRICT;
                END IF;
            END $$;

            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000042', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Xem chính sách lưu thông.', 'circulation-policies', 'circulation-policies.read'),
                ('30000000-0000-0000-0000-000000000043', TIMESTAMPTZ '2026-08-26T00:00:00Z', 'Quản lý chính sách lưu thông.', 'circulation-policies', 'circulation-policies.manage')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('circulation-policies.read', 'circulation-policies.manage')
            ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;

            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                SELECT "Id" FROM permissions
                WHERE "Name" IN ('circulation-policies.read', 'circulation-policies.manage'));
            DELETE FROM permissions
            WHERE "Name" IN ('circulation-policies.read', 'circulation-policies.manage');

            ALTER TABLE circulation_policies DROP CONSTRAINT IF EXISTS "FK_circulation_policies_branches_BranchId";
            DROP INDEX IF EXISTS "IX_circulation_policies_IsActive_EffectiveFrom_EffectiveTo";
            DROP INDEX IF EXISTS "IX_circulation_policies_MemberGroup_DocumentType_BranchId";
            DROP INDEX IF EXISTS "IX_circulation_policies_Name_Version";

            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "Description";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "Version";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "MemberGroup";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "DocumentType";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "BranchId";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "MaxLoanBooks";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "LoanPeriodDays";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "MaxRenewals";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "RenewalPeriodDays";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "HoldDays";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "BlockIfOverdue";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "FinePerDay";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "FixedFineAmount";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "MaxFineAmount";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "LostBookPenaltyRatio";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "CreatedAtUtc";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "UpdatedAtUtc";
            ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "CreatedByUserId";

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveFromUtc'
                ) THEN
                    ALTER TABLE circulation_policies RENAME COLUMN "EffectiveFrom" TO "EffectiveFromUtc";
                ELSE
                    UPDATE circulation_policies
                    SET "EffectiveFromUtc" = COALESCE("EffectiveFromUtc", "EffectiveFrom");
                    ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "EffectiveFrom";
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveToUtc'
                ) THEN
                    ALTER TABLE circulation_policies RENAME COLUMN "EffectiveTo" TO "EffectiveToUtc";
                ELSE
                    UPDATE circulation_policies
                    SET "EffectiveToUtc" = COALESCE("EffectiveToUtc", "EffectiveTo");
                    ALTER TABLE circulation_policies DROP COLUMN IF EXISTS "EffectiveTo";
                END IF;
            END $$;

            ALTER TABLE circulation_policies ALTER COLUMN "EffectiveFromUtc" SET NOT NULL;
            ALTER TABLE circulation_policies ALTER COLUMN "Name" TYPE character varying(150);
            """);
    }
}
