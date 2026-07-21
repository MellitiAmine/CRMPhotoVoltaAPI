using System;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmPhotoVolta.Infrastructure.Data.Migrations.App
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260623140000_CommercialTimeEntryPunchType")]
    public partial class CommercialTimeEntryPunchType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'app'
                          AND table_name = 'CommercialTimeEntries'
                          AND column_name = 'CheckOut'
                    ) THEN
                        ALTER TABLE app."CommercialTimeEntries"
                            ADD COLUMN IF NOT EXISTS "PunchType" integer NOT NULL DEFAULT 0;

                        ALTER TABLE app."CommercialTimeEntries"
                            ADD COLUMN IF NOT EXISTS "Time" time without time zone;

                        UPDATE app."CommercialTimeEntries"
                        SET "Time" = "CheckIn", "PunchType" = 0
                        WHERE "Time" IS NULL;

                        INSERT INTO app."CommercialTimeEntries" (
                            "Id", "SocietyId", "CommercialProfileId", "WorkDate",
                            "PunchType", "Time", "CheckIn", "CheckOut", "Notes",
                            "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
                        SELECT gen_random_uuid(), "SocietyId", "CommercialProfileId", "WorkDate",
                            1, "CheckOut", "CheckIn", "CheckOut", "Notes",
                            "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted"
                        FROM app."CommercialTimeEntries"
                        WHERE "CheckOut" > "CheckIn" AND NOT "IsDeleted";

                        ALTER TABLE app."CommercialTimeEntries"
                            ALTER COLUMN "Time" SET NOT NULL;

                        ALTER TABLE app."CommercialTimeEntries" DROP COLUMN "CheckIn";
                        ALTER TABLE app."CommercialTimeEntries" DROP COLUMN "CheckOut";
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'app'
                          AND table_name = 'CommercialTimeEntries'
                          AND column_name = 'Time'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'app'
                          AND table_name = 'CommercialTimeEntries'
                          AND column_name = 'CheckIn'
                    ) THEN
                        ALTER TABLE app."CommercialTimeEntries"
                            ADD COLUMN "CheckIn" time without time zone,
                            ADD COLUMN "CheckOut" time without time zone;

                        UPDATE app."CommercialTimeEntries"
                        SET "CheckIn" = "Time",
                            "CheckOut" = "Time"
                        WHERE "PunchType" = 0;

                        DELETE FROM app."CommercialTimeEntries" WHERE "PunchType" = 1;

                        ALTER TABLE app."CommercialTimeEntries"
                            ALTER COLUMN "CheckIn" SET NOT NULL,
                            ALTER COLUMN "CheckOut" SET NOT NULL;

                        ALTER TABLE app."CommercialTimeEntries" DROP COLUMN "PunchType";
                        ALTER TABLE app."CommercialTimeEntries" DROP COLUMN "Time";
                    END IF;
                END $$;
                """);
        }
    }
}
