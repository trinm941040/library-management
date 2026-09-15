using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UTH.Library.Infrastructure.Persistence;

#nullable disable

namespace UTH.Library.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LibraryDbContext))]
[Migration("20260913070000_SaveAppliedCirculationPolicy")]
public partial class SaveAppliedCirculationPolicy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE borrowings ADD COLUMN IF NOT EXISTS "RenewalCount" integer NOT NULL DEFAULT 0;
            ALTER TABLE borrowings ADD COLUMN IF NOT EXISTS "AppliedPolicyId" uuid;
            ALTER TABLE borrowings ADD COLUMN IF NOT EXISTS "AppliedPolicyVersion" integer NOT NULL DEFAULT 1;
            ALTER TABLE borrowings ADD COLUMN IF NOT EXISTS "AppliedPolicySnapshot" jsonb NOT NULL DEFAULT '{}'::jsonb;

            ALTER TABLE reservations ADD COLUMN IF NOT EXISTS "AppliedPolicyId" uuid;
            ALTER TABLE reservations ADD COLUMN IF NOT EXISTS "AppliedPolicyVersion" integer NOT NULL DEFAULT 1;
            ALTER TABLE reservations ADD COLUMN IF NOT EXISTS "AppliedPolicySnapshot" jsonb NOT NULL DEFAULT '{}'::jsonb;

            ALTER TABLE violations ADD COLUMN IF NOT EXISTS "AppliedPolicyId" uuid;
            ALTER TABLE violations ADD COLUMN IF NOT EXISTS "AppliedPolicyVersion" integer NOT NULL DEFAULT 1;
            ALTER TABLE violations ADD COLUMN IF NOT EXISTS "AppliedPolicySnapshot" jsonb NOT NULL DEFAULT '{}'::jsonb;

            ALTER TABLE renewals ADD COLUMN IF NOT EXISTS "AppliedPolicyId" uuid;
            ALTER TABLE renewals ADD COLUMN IF NOT EXISTS "AppliedPolicyVersion" integer NOT NULL DEFAULT 1;
            ALTER TABLE renewals ADD COLUMN IF NOT EXISTS "AppliedPolicySnapshot" jsonb NOT NULL DEFAULT '{}'::jsonb;

            CREATE INDEX IF NOT EXISTS "IX_borrowings_AppliedPolicyId" ON borrowings ("AppliedPolicyId");
            CREATE INDEX IF NOT EXISTS "IX_reservations_AppliedPolicyId" ON reservations ("AppliedPolicyId");
            CREATE INDEX IF NOT EXISTS "IX_violations_AppliedPolicyId" ON violations ("AppliedPolicyId");
            CREATE INDEX IF NOT EXISTS "IX_renewals_AppliedPolicyId" ON renewals ("AppliedPolicyId");

            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_borrowings_circulation_policies_AppliedPolicyId') THEN
                    ALTER TABLE borrowings ADD CONSTRAINT "FK_borrowings_circulation_policies_AppliedPolicyId" FOREIGN KEY ("AppliedPolicyId") REFERENCES circulation_policies ("Id") ON DELETE SET NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_reservations_circulation_policies_AppliedPolicyId') THEN
                    ALTER TABLE reservations ADD CONSTRAINT "FK_reservations_circulation_policies_AppliedPolicyId" FOREIGN KEY ("AppliedPolicyId") REFERENCES circulation_policies ("Id") ON DELETE SET NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_violations_circulation_policies_AppliedPolicyId') THEN
                    ALTER TABLE violations ADD CONSTRAINT "FK_violations_circulation_policies_AppliedPolicyId" FOREIGN KEY ("AppliedPolicyId") REFERENCES circulation_policies ("Id") ON DELETE SET NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_renewals_circulation_policies_AppliedPolicyId') THEN
                    ALTER TABLE renewals ADD CONSTRAINT "FK_renewals_circulation_policies_AppliedPolicyId" FOREIGN KEY ("AppliedPolicyId") REFERENCES circulation_policies ("Id") ON DELETE SET NULL;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE borrowings DROP CONSTRAINT IF EXISTS "FK_borrowings_circulation_policies_AppliedPolicyId";
            ALTER TABLE reservations DROP CONSTRAINT IF EXISTS "FK_reservations_circulation_policies_AppliedPolicyId";
            ALTER TABLE violations DROP CONSTRAINT IF EXISTS "FK_violations_circulation_policies_AppliedPolicyId";
            ALTER TABLE renewals DROP CONSTRAINT IF EXISTS "FK_renewals_circulation_policies_AppliedPolicyId";
            DROP INDEX IF EXISTS "IX_borrowings_AppliedPolicyId";
            DROP INDEX IF EXISTS "IX_reservations_AppliedPolicyId";
            DROP INDEX IF EXISTS "IX_violations_AppliedPolicyId";
            DROP INDEX IF EXISTS "IX_renewals_AppliedPolicyId";
            ALTER TABLE borrowings DROP COLUMN IF EXISTS "RenewalCount", DROP COLUMN IF EXISTS "AppliedPolicyId", DROP COLUMN IF EXISTS "AppliedPolicyVersion", DROP COLUMN IF EXISTS "AppliedPolicySnapshot";
            ALTER TABLE reservations DROP COLUMN IF EXISTS "AppliedPolicyId", DROP COLUMN IF EXISTS "AppliedPolicyVersion", DROP COLUMN IF EXISTS "AppliedPolicySnapshot";
            ALTER TABLE violations DROP COLUMN IF EXISTS "AppliedPolicyId", DROP COLUMN IF EXISTS "AppliedPolicyVersion", DROP COLUMN IF EXISTS "AppliedPolicySnapshot";
            ALTER TABLE renewals DROP COLUMN IF EXISTS "AppliedPolicyId", DROP COLUMN IF EXISTS "AppliedPolicyVersion", DROP COLUMN IF EXISTS "AppliedPolicySnapshot";
            """);
    }
}
