DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'core') THEN
        CREATE SCHEMA core;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS core."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'core') THEN
            CREATE SCHEMA core;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."Permissions" (
        "Id" uuid NOT NULL,
        "Code" character varying(128) NOT NULL,
        "Description" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."SubscriptionPlans" (
        "Id" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "Price" numeric NOT NULL,
        "MaxUsers" integer NOT NULL,
        "MaxProjects" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_SubscriptionPlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(320) NOT NULL,
        "PasswordHash" text NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Phone" character varying(50),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."Societies" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "SubscriptionPlanId" uuid,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Societies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Societies_SubscriptionPlans_SubscriptionPlanId" FOREIGN KEY ("SubscriptionPlanId") REFERENCES core."SubscriptionPlans" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "SocietyId" uuid NOT NULL,
        "Token" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES core."Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."Roles" (
        "Id" uuid NOT NULL,
        "SocietyId" uuid,
        "Name" character varying(100) NOT NULL,
        "IsSystemRole" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Roles_Societies_SocietyId" FOREIGN KEY ("SocietyId") REFERENCES core."Societies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."Subscriptions" (
        "Id" uuid NOT NULL,
        "SocietyId" uuid NOT NULL,
        "PlanId" uuid NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Subscriptions_Societies_SocietyId" FOREIGN KEY ("SocietyId") REFERENCES core."Societies" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Subscriptions_SubscriptionPlans_PlanId" FOREIGN KEY ("PlanId") REFERENCES core."SubscriptionPlans" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."RolePermissions" (
        "Id" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "PermissionId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES core."Permissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."Roles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE TABLE core."UserSocieties" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "SocietyId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_UserSocieties" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserSocieties_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES core."Roles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserSocieties_Societies_SocietyId" FOREIGN KEY ("SocietyId") REFERENCES core."Societies" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserSocieties_Users_UserId" FOREIGN KEY ("UserId") REFERENCES core."Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE UNIQUE INDEX "IX_Permissions_Code" ON core."Permissions" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON core."RefreshTokens" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_RefreshTokens_UserId" ON core."RefreshTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_RolePermissions_PermissionId" ON core."RolePermissions" ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_PermissionId" ON core."RolePermissions" ("RoleId", "PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_Roles_SocietyId" ON core."Roles" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_Societies_SubscriptionPlanId" ON core."Societies" ("SubscriptionPlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_Subscriptions_PlanId" ON core."Subscriptions" ("PlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_Subscriptions_SocietyId" ON core."Subscriptions" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON core."Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_UserSocieties_RoleId" ON core."UserSocieties" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE INDEX "IX_UserSocieties_SocietyId" ON core."UserSocieties" ("SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    CREATE UNIQUE INDEX "IX_UserSocieties_UserId_SocietyId" ON core."UserSocieties" ("UserId", "SocietyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412150732_InitialCore') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412150732_InitialCore', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    ALTER TABLE core."SubscriptionPlans" ADD "BillingPeriodMonths" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    ALTER TABLE core."SubscriptionPlans" ADD "Code" character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    ALTER TABLE core."SubscriptionPlans" ADD "Currency" character varying(8) NOT NULL DEFAULT 'TND';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    ALTER TABLE core."SubscriptionPlans" ADD "TrialDurationMonths" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    UPDATE core."SubscriptionPlans"
    SET "Code" = 'FREE_TRIAL_3M',
        "Currency" = 'TND',
        "TrialDurationMonths" = 3,
        "BillingPeriodMonths" = 3,
        "Price" = 0
    WHERE "Name" = 'Starter';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    CREATE UNIQUE INDEX "IX_SubscriptionPlans_Code" ON core."SubscriptionPlans" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    ALTER TABLE core."Users" ADD "IsPlatformAdministrator" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412154500_SubscriptionPlansAndPlatformAdmin') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412154500_SubscriptionPlansAndPlatformAdmin', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162145_DropIsPlatformAdministratorFromUsers') THEN
    ALTER TABLE core."Users" DROP COLUMN "IsPlatformAdministrator";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM core."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162145_DropIsPlatformAdministratorFromUsers') THEN
    INSERT INTO core."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412162145_DropIsPlatformAdministratorFromUsers', '8.0.11');
    END IF;
END $EF$;
COMMIT;

