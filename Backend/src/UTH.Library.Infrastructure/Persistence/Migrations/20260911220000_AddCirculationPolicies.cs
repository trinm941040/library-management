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
            CREATE TABLE IF NOT EXISTS circulation_policies (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(1000),
                "Version" integer NOT NULL,
                "IsActive" boolean NOT NULL,
                "MemberGroup" character varying(100),
                "DocumentType" character varying(100),
                "BranchId" uuid,
                "EffectiveFrom" timestamp with time zone NOT NULL,
                "EffectiveTo" timestamp with time zone,
                "MaxLoanBooks" integer NOT NULL,
                "LoanPeriodDays" integer NOT NULL,
                "MaxRenewals" integer NOT NULL,
                "RenewalPeriodDays" integer NOT NULL,
                "HoldDays" integer NOT NULL,
                "BlockIfOverdue" boolean NOT NULL,
                "FinePerDay" numeric(18,2) NOT NULL,
                "FixedFineAmount" numeric(18,2) NOT NULL,
                "MaxFineAmount" numeric(18,2) NOT NULL,
                "LostBookPenaltyRatio" numeric(18,2) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "CreatedByUserId" uuid,
                CONSTRAINT "PK_circulation_policies" PRIMARY KEY ("Id")
            );

            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Id" uuid;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Name" character varying(200);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Description" character varying(1000);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "Version" integer DEFAULT 1;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "IsActive" boolean DEFAULT true;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MemberGroup" character varying(100);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "DocumentType" character varying(100);
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "BranchId" uuid;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "EffectiveFrom" timestamp with time zone DEFAULT NOW();
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "EffectiveTo" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxLoanBooks" integer DEFAULT 5;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "LoanPeriodDays" integer DEFAULT 14;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxRenewals" integer DEFAULT 2;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "RenewalPeriodDays" integer DEFAULT 7;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "HoldDays" integer DEFAULT 3;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "BlockIfOverdue" boolean DEFAULT true;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "FinePerDay" numeric(18,2) DEFAULT 5000;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "FixedFineAmount" numeric(18,2) DEFAULT 0;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "MaxFineAmount" numeric(18,2) DEFAULT 100000;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "LostBookPenaltyRatio" numeric(18,2) DEFAULT 150;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp with time zone DEFAULT NOW();
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "UpdatedAtUtc" timestamp with time zone;
            ALTER TABLE circulation_policies ADD COLUMN IF NOT EXISTS "CreatedByUserId" uuid;

            CREATE INDEX IF NOT EXISTS "IX_circulation_policies_IsActive_EffectiveFrom_EffectiveTo"
            ON circulation_policies ("IsActive", "EffectiveFrom", "EffectiveTo");

            CREATE INDEX IF NOT EXISTS "IX_circulation_policies_MemberGroup_DocumentType_BranchId"
            ON circulation_policies ("MemberGroup", "DocumentType", "BranchId");

            INSERT INTO permissions ("Id", "CreatedAtUtc", "Description", "Module", "Name")
            VALUES
                ('30000000-0000-0000-0000-000000000030', TIMESTAMPTZ '2026-09-11T00:00:00Z', 'Read circulation policies.', 'circulation-policies', 'circulation-policies.read'),
                ('30000000-0000-0000-0000-000000000031', TIMESTAMPTZ '2026-09-11T00:00:00Z', 'Manage circulation policies.', 'circulation-policies', 'circulation-policies.manage')
            ON CONFLICT ("Name") DO UPDATE
            SET "Description" = EXCLUDED."Description", "Module" = EXCLUDED."Module";

            INSERT INTO role_permissions ("RoleId", "PermissionId")
            SELECT role."Id", permission."Id"
            FROM roles AS role
            CROSS JOIN permissions AS permission
            WHERE role."NormalizedName" = 'ADMINISTRATOR'
              AND permission."Name" IN ('circulation-policies.read', 'circulation-policies.manage')
            ON CONFLICT DO NOTHING;

            DO $$
            DECLARE
                rec RECORD;
            BEGIN
                -- Drop NOT NULL on any legacy columns (like EffectiveFromUtc) that would block inserts
                FOR rec IN 
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'circulation_policies' 
                      AND is_nullable = 'NO' 
                      AND column_default IS NULL
                      AND column_name NOT IN ('Id', 'Name')
                LOOP
                    EXECUTE format('ALTER TABLE circulation_policies ALTER COLUMN %I DROP NOT NULL', rec.column_name);
                END LOOP;
            END $$;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'PK_circulation_policies'
                ) THEN
                    BEGIN
                        ALTER TABLE circulation_policies ADD CONSTRAINT "PK_circulation_policies" PRIMARY KEY ("Id");
                    EXCEPTION
                        WHEN OTHERS THEN NULL;
                    END;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM circulation_policies WHERE "Id" = '50000000-0000-0000-0000-000000000001'
                ) THEN
                    INSERT INTO circulation_policies (
                        "Id", "Name", "Description", "Version", "IsActive", "MemberGroup", "DocumentType", "BranchId",
                        "EffectiveFrom", "EffectiveTo", "MaxLoanBooks", "LoanPeriodDays", "MaxRenewals", "RenewalPeriodDays",
                        "HoldDays", "BlockIfOverdue", "FinePerDay", "FixedFineAmount", "MaxFineAmount", "LostBookPenaltyRatio",
                        "CreatedAtUtc", "UpdatedAtUtc", "CreatedByUserId")
                    VALUES (
                        '50000000-0000-0000-0000-000000000001', 'Chính sách lưu thông chuẩn toàn thư viện', 'Chính sách mặc định áp dụng cho tất cả độc giả và thể loại sách.',
                        1, TRUE, NULL, NULL, NULL,
                        TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, 5, 14, 2, 7,
                        3, TRUE, 5000.00, 0.00, 100000.00, 150.00,
                        TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, NULL);

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'circulation_policies' AND column_name = 'EffectiveFromUtc'
                    ) THEN
                        UPDATE circulation_policies 
                        SET "EffectiveFromUtc" = "EffectiveFrom" 
                        WHERE "Id" = '50000000-0000-0000-0000-000000000001';
                    END IF;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "PermissionId" IN (
                '30000000-0000-0000-0000-000000000030',
                '30000000-0000-0000-0000-000000000031');

            DELETE FROM permissions
            WHERE "Name" IN ('circulation-policies.read', 'circulation-policies.manage');
            """);

        migrationBuilder.DropTable(name: "circulation_policies");
    }
}
