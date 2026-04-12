# Test accounts (CRM PhotoVolta API)

Passwords and emails come from configuration (`PlatformSeed` in `appsettings.json` or `appsettings.Development.json`). Defaults below match the root `appsettings.json` after a fresh seed.

## Two authentication worlds (no mixing)

| Side | Login | JWT audience | Typical use |
|------|--------|----------------|-------------|
| **Platform** (SaaS operator) | `POST /api/v1/platform/auth/login` | `CrmPhotoVoltaPlatform` (see `PlatformJwt:Audience`) | Societies, subscription plans, subscription rows under `/api/v1/platform/…` |
| **Tenant** (CRM per society) | `POST /api/v1/auth/login` | `CrmPhotoVoltaClients` (see `Jwt:Audience`) | Users, roles, societies **where the user is a member**, `switch-society`, tenant CRM APIs |

Platform operators are stored in schema **`platform`** (`PlatformUsers`, roles, permissions). Tenant users live in schema **`core`** (`Users`, `UserSocieties`, …). There is **no** `IsPlatformAdministrator` on tenant users.

## Default emails and passwords (change in production)

| # | Email | Password | Auth | Notes |
|---|--------|----------|------|--------|
| 1 | `plateforme@crm.local` | `ChangeMe123!` | **Platform** `POST /api/v1/platform/auth/login` | `SuperAdmin` in `platform` schema; not a tenant user after seed (legacy tenant row with the same email is soft-deleted). |
| 2 | `admin.essai@crm.local` | `Demo123!` | **Tenant** `POST /api/v1/auth/login` | Admin of the free-trial demo society only. |
| 3 | `admin.payant@crm.local` | `Demo123!` | **Tenant** `POST /api/v1/auth/login` | Admin of the paid-plan demo society only. |

Society display names are controlled by `DemoSocietyFreeName` and `DemoSocietyPaidName`.

## Useful API calls

- **Tenant login:** `POST /api/v1/auth/login` → response includes `accessToken`, `refreshToken`, `userId`, `societyId`, `role` (first active membership). Use `Authorization: Bearer` with the **tenant** token for `/api/v1/…` (except `/platform/…`).
- **Platform login:** `POST /api/v1/platform/auth/login` → `accessToken`, `platformUserId`, `roles`. Use **platform** token for `/api/v1/platform/…` only.
- **Who am I (tenant):** `GET /api/v1/auth/me` → list of `societies` (memberships only).
- **Switch society (tenant only):** `POST /api/v1/auth/switch-society` with `{ "societyId" }`.
- **Platform – societies:** `GET|POST /api/v1/platform/societies`, `PUT|DELETE /api/v1/platform/societies/{id}`.
- **Platform – plans:** `GET|POST /api/v1/platform/subscription-plans`, `PUT /api/v1/platform/subscription-plans/{planId}`.
- **Platform – subscription row:** `PUT /api/v1/platform/subscriptions/{id}`.

## CRM per society (tenant JWT + `society_id`)

All routes below require the **tenant** token. Data is stored in schema **`app`** with `SocietyId` = current JWT society. List endpoints support `page`, `pageSize` (10–50), `sortBy`, `sortOrder`, `search` like `/api/v1/users`.

| Resource | Routes |
|----------|--------|
| **Leads** | `GET/POST /api/v1/leads`, `GET/PUT/DELETE /api/v1/leads/{id}`, `GET/POST /api/v1/leads/{id}/activities` |
| **Clients** | `GET/POST /api/v1/clients`, `GET/PUT/DELETE /api/v1/clients/{id}` |
| **Deals** | `GET/POST /api/v1/deals`, `GET/PUT/DELETE /api/v1/deals/{id}` (optional `leadId`, assignee must be a user of the society) |
| **Projects** | `GET/POST /api/v1/projects`, `GET/PUT/DELETE /api/v1/projects/{id}` — `GET` supports `?clientId=` filter; `clientId` / `dealId` must belong to the same society |

## CRM PRO (tenant JWT)

Same auth as above. New or extended routes for dashboard, quotes, pipeline, installations, mobile “me”, calendar, notifications, documents, reports, society settings, and role permissions.

| Area | Routes |
|------|--------|
| **Dashboard** | `GET /api/v1/dashboard/overview`, `GET /api/v1/dashboard/kpis`, `GET /api/v1/dashboard/revenue`, `GET /api/v1/dashboard/pipeline`, `GET /api/v1/dashboard/projects` |
| **Leads (lifecycle)** | `POST /api/v1/leads/{id}/assign`, `POST /api/v1/leads/{id}/convert`, `POST /api/v1/leads/{id}/mark-won`, `POST /api/v1/leads/{id}/mark-lost`, `POST /api/v1/leads/{id}/notes`, `GET /api/v1/leads/{id}/timeline` (plus existing CRUD and `…/activities`) |
| **Quotes** | `GET/POST /api/v1/quotes`, `GET/PUT/DELETE /api/v1/quotes/{id}`, `POST /api/v1/quotes/{id}/send`, `…/accept`, `…/reject`, `…/convert-to-project` |
| **Pipeline stages** | `GET/POST /api/v1/pipeline/stages`, `PUT/DELETE /api/v1/pipeline/stages/{id}` |
| **Projects (PM)** | `GET /api/v1/projects/{id}/overview`, `GET /api/v1/projects/{id}/progress`, `POST /api/v1/projects/{id}/assign-technician`, `POST /api/v1/projects/{id}/assign-manager`, `PATCH /api/v1/projects/{id}/progress` |
| **Installations (terrain)** | `GET /api/v1/installations/{id}`, `POST /api/v1/installations/{id}/start`, `POST /api/v1/installations/{id}/complete`, `PUT /api/v1/installations/{id}/checklist`, `POST /api/v1/installations/{id}/photos` |
| **Technician / mobile** | `GET /api/v1/me/tasks`, `GET /api/v1/me/installations`, `GET /api/v1/me/schedule?from=&to=` |
| **Calendar** | `GET /api/v1/calendar?from=&to=&technicianId=&projectId=`, `GET /api/v1/calendar/technicians/{id}`, `GET /api/v1/calendar/projects/{id}` |
| **Notifications** | `GET /api/v1/notifications`, `POST /api/v1/notifications/read` |
| **Documents** | `POST /api/v1/documents/upload` (multipart), `GET /api/v1/documents/projects/{projectId}`, `GET /api/v1/documents/clients/{clientId}` — files are stored under `uploads/{societyId}/…` and served at **`/uploads/...`** (static files) |
| **Reports** | `GET /api/v1/reports/sales`, `GET /api/v1/reports/projects`, `GET /api/v1/reports/technicians`, `GET /api/v1/reports/conversion` |
| **Society settings** | `GET /api/v1/settings`, `PUT /api/v1/settings` (JSON payload per app contract) |
| **Roles** | Existing `GET/POST /api/v1/roles`, `PUT/DELETE /api/v1/roles/{id}` plus `GET /api/v1/roles/{id}/permissions`, `PUT /api/v1/roles/{id}/permissions` |
| **Role name ideas (catalog)** | `GET /api/v1/roles/catalog/commercial-suggestions` — suggested commercial / org role **names** only (not persisted RBAC) |

## Seeding

On startup: **Core** and **App** and **Platform** EF migrations run, then `DatabaseSeeder` (core catalog), then `PlatformDatabaseSeeder` (platform roles/permissions), then if `PlatformSeed:Enabled` the platform super-admin user is created, legacy tenant user with the same email as the platform operator is removed from **core**, and `PlatformDemoSeeder` can create the two demo societies (`CreateDemoSocieties`).

Disable demo societies with `"CreateDemoSocieties": false`. Disable all optional demo behaviour with `"PlatformSeed:Enabled": false` (platform RBAC seed still runs; you must create the first `PlatformUser` yourself or re-enable seed temporarily).

---

# Comptes de test (résumé FR)

- **Plateforme** : `POST /api/v1/platform/auth/login` avec `plateforme@crm.local` — APIs sous `/api/v1/platform/…`.
- **Admins sociétés** : `POST /api/v1/auth/login` avec `admin.essai@crm.local` ou `admin.payant@crm.local` — CRM réservé à leur société ; `switch-society` uniquement si l’utilisateur a plusieurs sociétés. Données CRM : `/api/v1/leads`, `/clients`, `/deals`, `/projects`, et les routes **CRM PRO** (dashboard, devis, pipeline, installations, `me`, calendrier, notifications, documents, rapports, paramètres, permissions rôles — voir tableau ci-dessus).

Les mots de passe par défaut sont dans `PlatformSeed` et doivent être changés en production. Utilisez des clés JWT distinctes et longues pour `Jwt:SigningKey` et `PlatformJwt:SigningKey`.
