-- Run against the app database if API fails until Docker image is rebuilt with migration metadata.
-- Safe to run multiple times on PostgreSQL 12+.

ALTER TABLE app."Events" ADD COLUMN IF NOT EXISTS "LeadId" uuid NULL;

CREATE INDEX IF NOT EXISTS "IX_Events_LeadId" ON app."Events" ("LeadId");

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'FK_Events_Leads_LeadId'
  ) THEN
    ALTER TABLE app."Events"
      ADD CONSTRAINT "FK_Events_Leads_LeadId"
      FOREIGN KEY ("LeadId") REFERENCES app."Leads" ("Id") ON DELETE SET NULL;
  END IF;
END $$;
