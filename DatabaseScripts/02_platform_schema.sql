DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'platform') THEN
        CREATE SCHEMA platform;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS platform."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'platform') THEN
            CREATE SCHEMA platform;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE TABLE platform."PlatformPermissions" (
        "Id" uuid NOT NULL,
        "Code" character varying(128) NOT NULL,
        "Description" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PlatformPermissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE TABLE platform."PlatformRoles" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PlatformRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE TABLE platform."PlatformUsers" (
        "Id" uuid NOT NULL,
        "Email" character varying(320) NOT NULL,
        "PasswordHash" text NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PlatformUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE TABLE platform."PlatformRolePermissions" (
        "Id" uuid NOT NULL,
        "PlatformRoleId" uuid NOT NULL,
        "PlatformPermissionId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PlatformRolePermissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PlatformRolePermissions_PlatformPermissions_PlatformPermiss~" FOREIGN KEY ("PlatformPermissionId") REFERENCES platform."PlatformPermissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PlatformRolePermissions_PlatformRoles_PlatformRoleId" FOREIGN KEY ("PlatformRoleId") REFERENCES platform."PlatformRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE TABLE platform."PlatformUserRoles" (
        "Id" uuid NOT NULL,
        "PlatformUserId" uuid NOT NULL,
        "PlatformRoleId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedById" uuid,
        "UpdatedAt" timestamp with time zone,
        "UpdatedById" uuid,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PlatformUserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PlatformUserRoles_PlatformRoles_PlatformRoleId" FOREIGN KEY ("PlatformRoleId") REFERENCES platform."PlatformRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PlatformUserRoles_PlatformUsers_PlatformUserId" FOREIGN KEY ("PlatformUserId") REFERENCES platform."PlatformUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE UNIQUE INDEX "IX_PlatformPermissions_Code" ON platform."PlatformPermissions" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE INDEX "IX_PlatformRolePermissions_PlatformPermissionId" ON platform."PlatformRolePermissions" ("PlatformPermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE UNIQUE INDEX "IX_PlatformRolePermissions_PlatformRoleId_PlatformPermissionId" ON platform."PlatformRolePermissions" ("PlatformRoleId", "PlatformPermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE UNIQUE INDEX "IX_PlatformRoles_Name" ON platform."PlatformRoles" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE INDEX "IX_PlatformUserRoles_PlatformRoleId" ON platform."PlatformUserRoles" ("PlatformRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE UNIQUE INDEX "IX_PlatformUserRoles_PlatformUserId_PlatformRoleId" ON platform."PlatformUserRoles" ("PlatformUserId", "PlatformRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    CREATE UNIQUE INDEX "IX_PlatformUsers_Email" ON platform."PlatformUsers" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform."__EFMigrationsHistory" WHERE "MigrationId" = '20260412162104_InitialPlatform') THEN
    INSERT INTO platform."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412162104_InitialPlatform', '8.0.11');
    END IF;
END $EF$;
COMMIT;

