DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'app') THEN
        CREATE SCHEMA app;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS app."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'app') THEN
            CREATE SCHEMA app;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Clients" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Phone" text,
        "Email" text,
        "Address" text,
        "UserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Documents" (
        "Id" uuid NOT NULL,
        "ProjectId" uuid,
        "ClientId" uuid,
        "Type" text NOT NULL,
        "FileUrl" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Documents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Events" (
        "Id" uuid NOT NULL,
        "Title" text NOT NULL,
        "Type" text NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "AssignedToUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Events" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Leads" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Phone" text,
        "Email" text,
        "Address" text,
        "Status" text NOT NULL,
        "AssignedToUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Leads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."PipelineStages" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Order" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_PipelineStages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."ProjectStages" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Order" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_ProjectStages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Deals" (
        "Id" uuid NOT NULL,
        "LeadId" uuid,
        "Title" text NOT NULL,
        "Value" numeric,
        "Stage" text NOT NULL,
        "AssignedToUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Deals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Deals_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES app."Leads" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."LeadActivities" (
        "Id" uuid NOT NULL,
        "LeadId" uuid NOT NULL,
        "Type" text NOT NULL,
        "Notes" text,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_LeadActivities" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LeadActivities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES app."Leads" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Projects" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "DealId" uuid,
        "Name" text NOT NULL,
        "Address" text,
        "Status" text NOT NULL,
        "SystemSizeKw" numeric,
        "EstimatedProduction" numeric,
        "StartDate" date,
        "EndDate" date,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Projects" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Projects_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES app."Clients" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Projects_Deals_DealId" FOREIGN KEY ("DealId") REFERENCES app."Deals" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Installations" (
        "Id" uuid NOT NULL,
        "ProjectId" uuid NOT NULL,
        "TechnicianId" uuid NOT NULL,
        "Date" date NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Installations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Installations_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES app."Projects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."ProjectStageTracking" (
        "Id" uuid NOT NULL,
        "ProjectId" uuid NOT NULL,
        "StageId" uuid NOT NULL,
        "Status" text NOT NULL,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_ProjectStageTracking" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProjectStageTracking_ProjectStages_StageId" FOREIGN KEY ("StageId") REFERENCES app."ProjectStages" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ProjectStageTracking_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES app."Projects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."Tasks" (
        "Id" uuid NOT NULL,
        "ProjectId" uuid NOT NULL,
        "Title" text NOT NULL,
        "AssignedToUserId" uuid,
        "Status" text NOT NULL,
        "DueDate" date,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Tasks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Tasks_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES app."Projects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."InstallationChecklist" (
        "Id" uuid NOT NULL,
        "InstallationId" uuid NOT NULL,
        "Item" text NOT NULL,
        "IsCompleted" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_InstallationChecklist" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InstallationChecklist_Installations_InstallationId" FOREIGN KEY ("InstallationId") REFERENCES app."Installations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE TABLE app."InstallationPhotos" (
        "Id" uuid NOT NULL,
        "InstallationId" uuid NOT NULL,
        "Url" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_InstallationPhotos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InstallationPhotos_Installations_InstallationId" FOREIGN KEY ("InstallationId") REFERENCES app."Installations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Clients_SocietyId" ON app."Clients" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Deals_LeadId" ON app."Deals" ("LeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Deals_SocietyId" ON app."Deals" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Documents_SocietyId" ON app."Documents" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Events_SocietyId" ON app."Events" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_InstallationChecklist_InstallationId" ON app."InstallationChecklist" ("InstallationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_InstallationPhotos_InstallationId" ON app."InstallationPhotos" ("InstallationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Installations_ProjectId" ON app."Installations" ("ProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Installations_SocietyId" ON app."Installations" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_LeadActivities_LeadId" ON app."LeadActivities" ("LeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_LeadActivities_SocietyId" ON app."LeadActivities" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Leads_SocietyId" ON app."Leads" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_PipelineStages_SocietyId" ON app."PipelineStages" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Projects_ClientId" ON app."Projects" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Projects_DealId" ON app."Projects" ("DealId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Projects_SocietyId" ON app."Projects" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_ProjectStages_SocietyId" ON app."ProjectStages" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_ProjectStageTracking_ProjectId" ON app."ProjectStageTracking" ("ProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_ProjectStageTracking_StageId" ON app."ProjectStageTracking" ("StageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Tasks_ProjectId" ON app."Tasks" ("ProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    CREATE INDEX "IX_Tasks_SocietyId" ON app."Tasks" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150740_InitialApp') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412150740_InitialApp', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    ALTER TABLE app."Projects" ADD "ManagerUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    ALTER TABLE app."Projects" ADD "ProgressPercent" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    ALTER TABLE app."Projects" ADD "TechnicianUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE TABLE app."Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Body" text NOT NULL,
        "Type" character varying(64) NOT NULL,
        "ReadAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE TABLE app."Quotes" (
        "Id" uuid NOT NULL,
        "LeadId" uuid,
        "ClientId" uuid,
        "DealId" uuid,
        "ProjectId" uuid,
        "QuoteNumber" character varying(64) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Status" character varying(40) NOT NULL,
        "Currency" character varying(8) NOT NULL,
        "TotalAmount" numeric NOT NULL,
        "ValidUntil" date,
        "SentAt" timestamp with time zone,
        "AcceptedAt" timestamp with time zone,
        "RejectedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_Quotes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Quotes_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES app."Clients" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Quotes_Deals_DealId" FOREIGN KEY ("DealId") REFERENCES app."Deals" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Quotes_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES app."Leads" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Quotes_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES app."Projects" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE TABLE app."SocietySettings" (
        "Id" uuid NOT NULL,
        "DataJson" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_SocietySettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE TABLE app."QuoteItems" (
        "Id" uuid NOT NULL,
        "QuoteId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Quantity" numeric NOT NULL,
        "UnitPrice" numeric NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        "SocietyId" uuid NOT NULL,
        CONSTRAINT "PK_QuoteItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_QuoteItems_Quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES app."Quotes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Notifications_Society_User" ON app."Notifications" ("SocietyId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_QuoteItems_QuoteId" ON app."QuoteItems" ("QuoteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_QuoteItems_SocietyId" ON app."QuoteItems" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Quotes_ClientId" ON app."Quotes" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Quotes_DealId" ON app."Quotes" ("DealId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Quotes_LeadId" ON app."Quotes" ("LeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Quotes_ProjectId" ON app."Quotes" ("ProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE INDEX "IX_Quotes_SocietyId" ON app."Quotes" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE UNIQUE INDEX "IX_Quotes_SocietyId_QuoteNumber" ON app."Quotes" ("SocietyId", "QuoteNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    CREATE UNIQUE INDEX "IX_SocietySettings_SocietyId" ON app."SocietySettings" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM app."__EFMigrationsHistory" WHERE "MigrationId" = '20260412165518_AddProCrmQuotesNotificationsSettings') THEN
    INSERT INTO app."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412165518_AddProCrmQuotesNotificationsSettings', '8.0.11');
    END IF;
END $EF$;
COMMIT;

