-- =============================================================================
-- CRM PhotoVolta — reference + demo seed (PostgreSQL)
-- Mirrors: DatabaseSeeder, PlatformDatabaseSeeder, PlatformDemoSeeder + defaults from PlatformSeedOptions.
--
-- Passwords: all "PasswordHash" columns are set to '' (empty). You MUST set
-- BCrypt hashes before login works (e.g. via app maintenance API or SQL update).
--
-- Scopes: core (permissions, plans, demo societies/users) + platform (RBAC, platform admin).
--         app.* (leads, quotes, …) is NOT seeded by those C# classes — not included here.
--
-- Run after migrations. Idempotent: safe to re-run (ON CONFLICT / NOT EXISTS).
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- core: Permissions (DatabaseSeeder)
-- -----------------------------------------------------------------------------
INSERT INTO core."Permissions" ("Id", "Code", "Description", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  ('a0004001-0001-4001-8001-000000000001'::uuid, 'VIEW_PROJECT', 'View projects', now(), NULL, NULL, NULL, false),
  ('a0004002-0002-4002-8002-000000000002'::uuid, 'MANAGE_PROJECT', 'Manage projects', now(), NULL, NULL, NULL, false),
  ('a0004003-0003-4003-8003-000000000003'::uuid, 'VIEW_LEAD', 'View leads', now(), NULL, NULL, NULL, false),
  ('a0004004-0004-4004-8004-000000000004'::uuid, 'MANAGE_LEAD', 'Manage leads', now(), NULL, NULL, NULL, false),
  ('a0004005-0005-4005-8005-000000000005'::uuid, 'MANAGE_USERS', 'Manage users', now(), NULL, NULL, NULL, false),
  ('a0004006-0006-4006-8006-000000000006'::uuid, 'MANAGE_ROLES', 'Manage roles', now(), NULL, NULL, NULL, false),
  ('a0004007-0007-4007-8007-000000000007'::uuid, 'MANAGE_SOCIETY', 'Manage society settings', now(), NULL, NULL, NULL, false),
  ('a0004008-0008-4008-8008-000000000008'::uuid, 'PLATFORM_MANAGE_SOCIETIES', 'List and configure all societies and their subscription plans', now(), NULL, NULL, NULL, false)
ON CONFLICT ("Code") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: Subscription plans (DatabaseSeeder)
-- -----------------------------------------------------------------------------
INSERT INTO core."SubscriptionPlans" (
  "Id", "Code", "Name", "Currency", "Price", "TrialDurationMonths", "BillingPeriodMonths",
  "MaxUsers", "MaxProjects", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted"
)
VALUES
  (
    'b0004001-0001-4001-8001-000000000001'::uuid,
    'FREE_TRIAL_3M',
    'Essai gratuit — 3 mois',
    'TND',
    0,
    3,
    3,
    5,
    20,
    now(), NULL, NULL, NULL, false
  ),
  (
    'b0004002-0002-4002-8002-000000000002'::uuid,
    'STANDARD_100_M',
    'Formule standard — 100 TND / mois',
    'TND',
    100,
    NULL,
    1,
    50,
    500,
    now(), NULL, NULL, NULL, false
  )
ON CONFLICT ("Code") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: Demo societies (PlatformDemoSeeder + PlatformSeedOptions names)
-- -----------------------------------------------------------------------------
INSERT INTO core."Societies" ("Id", "Name", "SubscriptionPlanId", "IsActive", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  (
    'c0004001-0001-4001-8001-000000000001'::uuid,
    'Société test — Essai gratuit (3 mois)',
    'b0004001-0001-4001-8001-000000000001'::uuid,
    true,
    now(), NULL, NULL, NULL, false
  ),
  (
    'c0004002-0002-4002-8002-000000000002'::uuid,
    'Société test — Abonnement 100 TND/mois',
    'b0004002-0002-4002-8002-000000000002'::uuid,
    true,
    now(), NULL, NULL, NULL, false
  )
ON CONFLICT ("Id") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: Tenant demo users (emails from PlatformSeedOptions; PasswordHash empty)
-- -----------------------------------------------------------------------------
INSERT INTO core."Users" ("Id", "Email", "PasswordHash", "FullName", "Phone", "IsActive", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  (
    'd0004001-0001-4001-8001-000000000001'::uuid,
    'admin.essai@crm.local',
    '',
    'Admin société (essai gratuit)',
    NULL,
    true,
    now(), NULL, NULL, NULL, false
  ),
  (
    'd0004002-0002-4002-8002-000000000002'::uuid,
    'admin.payant@crm.local',
    '',
    'Admin société (abonnement)',
    NULL,
    true,
    now(), NULL, NULL, NULL, false
  )
ON CONFLICT ("Email") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: Admin role per demo society (RoleBootstrapper)
-- -----------------------------------------------------------------------------
INSERT INTO core."Roles" ("Id", "SocietyId", "Name", "IsSystemRole", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  ('e0004001-0001-4001-8001-000000000001'::uuid, 'c0004001-0001-4001-8001-000000000001'::uuid, 'Admin', false, now(), NULL, NULL, NULL, false),
  ('e0004002-0002-4002-8002-000000000002'::uuid, 'c0004002-0002-4002-8002-000000000002'::uuid, 'Admin', false, now(), NULL, NULL, NULL, false)
ON CONFLICT ("Id") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: RolePermissions — each seeded Admin role gets all current tenant permissions (by Code, works if Permission Ids differ from this script)
-- -----------------------------------------------------------------------------
INSERT INTO core."RolePermissions" ("Id", "RoleId", "PermissionId", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
SELECT gen_random_uuid(), r."Id", p."Id", now(), NULL, NULL, NULL, false
FROM core."Roles" r
CROSS JOIN core."Permissions" p
WHERE r."Id" IN ('e0004001-0001-4001-8001-000000000001'::uuid, 'e0004002-0002-4002-8002-000000000002'::uuid)
  AND NOT EXISTS (
    SELECT 1
    FROM core."RolePermissions" x
    WHERE x."RoleId" = r."Id"
      AND x."PermissionId" = p."Id"
      AND NOT x."IsDeleted"
  );

-- -----------------------------------------------------------------------------
-- core: UserSocieties (demo admins linked to societies + Admin role)
-- -----------------------------------------------------------------------------
INSERT INTO core."UserSocieties" ("Id", "UserId", "SocietyId", "RoleId", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  ('10004001-0001-4001-8001-000000000001'::uuid, 'd0004001-0001-4001-8001-000000000001'::uuid, 'c0004001-0001-4001-8001-000000000001'::uuid, 'e0004001-0001-4001-8001-000000000001'::uuid, now(), NULL, NULL, NULL, false),
  ('10004002-0002-4002-8002-000000000002'::uuid, 'd0004002-0002-4002-8002-000000000002'::uuid, 'c0004002-0002-4002-8002-000000000002'::uuid, 'e0004002-0002-4002-8002-000000000002'::uuid, now(), NULL, NULL, NULL, false)
ON CONFLICT ("UserId", "SocietyId") DO NOTHING;

-- -----------------------------------------------------------------------------
-- core: Subscriptions (PlatformDemoSeeder — period = COALESCE(TrialDurationMonths, BillingPeriodMonths) months)
-- -----------------------------------------------------------------------------
-- EndDate matches SubscriptionPeriodCalculator: COALESCE(TrialDurationMonths, BillingPeriodMonths) months from start.
INSERT INTO core."Subscriptions" (
  "Id", "SocietyId", "PlanId", "StartDate", "EndDate", "Status",
  "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted"
)
VALUES
  (
    '11004001-0001-4001-8001-000000000001'::uuid,
    'c0004001-0001-4001-8001-000000000001'::uuid,
    'b0004001-0001-4001-8001-000000000001'::uuid,
    CURRENT_DATE,
    (CURRENT_DATE + (3 * INTERVAL '1 month'))::date,
    'Active',
    now(), NULL, NULL, NULL, false
  ),
  (
    '11004002-0002-4002-8002-000000000002'::uuid,
    'c0004002-0002-4002-8002-000000000002'::uuid,
    'b0004002-0002-4002-8002-000000000002'::uuid,
    CURRENT_DATE,
    (CURRENT_DATE + (1 * INTERVAL '1 month'))::date,
    'Active',
    now(), NULL, NULL, NULL, false
  )
ON CONFLICT ("Id") DO NOTHING;

-- -----------------------------------------------------------------------------
-- platform: Permissions (PlatformDatabaseSeeder)
-- -----------------------------------------------------------------------------
INSERT INTO platform."PlatformPermissions" ("Id", "Code", "Description", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  ('20004001-0001-4001-8001-000000000001'::uuid, 'MANAGE_SOCIETIES', 'Create and manage tenant societies', now(), NULL, NULL, NULL, false),
  ('20004002-0002-4002-8002-000000000002'::uuid, 'MANAGE_SUBSCRIPTIONS', 'Manage society subscriptions', now(), NULL, NULL, NULL, false),
  ('20004003-0003-4003-8003-000000000003'::uuid, 'MANAGE_SUBSCRIPTION_PLANS', 'Manage subscription plan catalog', now(), NULL, NULL, NULL, false)
ON CONFLICT ("Code") DO NOTHING;

-- -----------------------------------------------------------------------------
-- platform: SuperAdmin role
-- -----------------------------------------------------------------------------
INSERT INTO platform."PlatformRoles" ("Id", "Name", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  ('21004001-0001-4001-8001-000000000001'::uuid, 'SuperAdmin', now(), NULL, NULL, NULL, false)
ON CONFLICT ("Name") DO NOTHING;

-- Link SuperAdmin to all platform permissions (by role name + perm codes for idempotency)
INSERT INTO platform."PlatformRolePermissions" ("Id", "PlatformRoleId", "PlatformPermissionId", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
SELECT gen_random_uuid(),
       r."Id",
       p."Id",
       now(), NULL, NULL, NULL, false
FROM platform."PlatformRoles" r
CROSS JOIN platform."PlatformPermissions" p
WHERE r."Name" = 'SuperAdmin'
  AND NOT EXISTS (
    SELECT 1
    FROM platform."PlatformRolePermissions" x
    WHERE x."PlatformRoleId" = r."Id"
      AND x."PlatformPermissionId" = p."Id"
      AND NOT x."IsDeleted"
  );

-- -----------------------------------------------------------------------------
-- platform: Platform operator (EnsureSuperAdminUserAsync — PasswordHash empty)
-- -----------------------------------------------------------------------------
INSERT INTO platform."PlatformUsers" ("Id", "Email", "PasswordHash", "FullName", "IsActive", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
VALUES
  (
    '22004001-0001-4001-8001-000000000001'::uuid,
    'plateforme@crm.local',
    '',
    'Super administrateur plateforme',
    true,
    now(), NULL, NULL, NULL, false
  )
ON CONFLICT ("Email") DO NOTHING;

INSERT INTO platform."PlatformUserRoles" ("Id", "PlatformUserId", "PlatformRoleId", "CreatedAt", "CreatedById", "UpdatedAt", "UpdatedById", "IsDeleted")
SELECT gen_random_uuid(), u."Id", r."Id", now(), NULL, NULL, NULL, false
FROM platform."PlatformUsers" u
CROSS JOIN platform."PlatformRoles" r
WHERE u."Email" = 'plateforme@crm.local'
  AND r."Name" = 'SuperAdmin'
  AND NOT EXISTS (
    SELECT 1
    FROM platform."PlatformUserRoles" x
    WHERE x."PlatformUserId" = u."Id"
      AND x."PlatformRoleId" = r."Id"
      AND NOT x."IsDeleted"
  );

COMMIT;
