# Test accounts (CRM PhotoVolta API)

Passwords and emails come from configuration (`PlatformSeed` in `appsettings.json` or `appsettings.Development.json`). Defaults below match the root `appsettings.json` after a fresh seed.

## Two authentication worlds (no mixing)

| Side | Login | JWT audience | Typical use |
|------|--------|----------------|-------------|
| **Platform** (SaaS operator) | `POST /api/v1/platform/auth/login` | `CrmPhotoVoltaPlatform` (see `PlatformJwt:Audience`) | Societies, subscription plans, subscription rows under `/api/v1/platform/…` |
| **Tenant** (CRM per society) | `POST /api/v1/auth/login` | `CrmPhotoVoltaClients` (see `Jwt:Audience`) | Users, roles, current society via JWT/`/auth/me`, tenant CRM APIs (societies are **not** created from tenant APIs) |

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
- **Who am I (tenant):** `GET /api/v1/auth/me` → current society context (one membership per account; invalid multi-membership returns `403`).
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
- **Admins sociétés** : `POST /api/v1/auth/login` avec `admin.essai@crm.local` ou `admin.payant@crm.local` — CRM réservé à leur société (une société par compte). Données CRM : `/api/v1/leads`, `/clients`, `/deals`, `/projects`, et les routes **CRM PRO** (dashboard, devis, pipeline, installations, `me`, calendrier, notifications, documents, rapports, paramètres, permissions rôles — voir tableau ci-dessus).

Les mots de passe par défaut sont dans `PlatformSeed` et doivent être changés en production. Utilisez des clés JWT distinctes et longues pour `Jwt:SigningKey` et `PlatformJwt:SigningKey`.

---

# Full API documentation (single file)

Base URL: `/api/v1`

## Health

- `GET /api/health`

## Tenant Auth

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`

## Platform Auth

- `POST /api/v1/platform/auth/login`

## Tenant - Core admin

### Users
- `GET /api/v1/users`
- `GET /api/v1/users/{id}`
- `POST /api/v1/users`
- `PUT /api/v1/users/{id}`
- `DELETE /api/v1/users/{id}`
- `POST /api/v1/users/{id}/assign-role`

### Roles & permissions
- `GET /api/v1/roles`
- `POST /api/v1/roles`
- `PUT /api/v1/roles/{id}`
- `DELETE /api/v1/roles/{id}`
- `GET /api/v1/roles/{id}/permissions`
- `PUT /api/v1/roles/{id}/permissions`
- `GET /api/v1/roles/catalog/commercial-suggestions`
- `GET /api/v1/permissions`

### Subscriptions (tenant side)
- `GET /api/v1/subscriptions/current`
- `POST /api/v1/subscriptions/upgrade`

## Platform admin

### Platform societies
- `GET /api/v1/platform/societies`
- `GET /api/v1/platform/societies/{id}`
- `POST /api/v1/platform/societies`
- `PUT /api/v1/platform/societies/{id}`
- `DELETE /api/v1/platform/societies/{id}`

### Platform plans
- `GET /api/v1/platform/subscription-plans`
- `POST /api/v1/platform/subscription-plans`
- `PUT /api/v1/platform/subscription-plans/{id}`

### Platform subscriptions
- `PUT /api/v1/platform/subscriptions/{id}`

## CRM (tenant)

### Leads
- `GET /api/v1/leads`
- `GET /api/v1/leads/{id}`
- `POST /api/v1/leads`
- `PUT /api/v1/leads/{id}`
- `DELETE /api/v1/leads/{id}`
- `GET /api/v1/leads/{id}/activities`
- `POST /api/v1/leads/{id}/activities`
- `POST /api/v1/leads/{id}/assign`
- `POST /api/v1/leads/{id}/convert`
- `POST /api/v1/leads/{id}/mark-won`
- `POST /api/v1/leads/{id}/mark-lost`
- `POST /api/v1/leads/{id}/notes`
- `GET /api/v1/leads/{id}/timeline`

### Clients
- `GET /api/v1/clients`
- `GET /api/v1/clients/{id}`
- `POST /api/v1/clients`
- `PUT /api/v1/clients/{id}`
- `DELETE /api/v1/clients/{id}`

### Deals
- `GET /api/v1/deals`
- `GET /api/v1/deals/{id}`
- `POST /api/v1/deals`
- `PUT /api/v1/deals/{id}`
- `DELETE /api/v1/deals/{id}`

### Quotes
- `GET /api/v1/quotes`
- `GET /api/v1/quotes/{id}`
- `POST /api/v1/quotes`
- `PUT /api/v1/quotes/{id}`
- `DELETE /api/v1/quotes/{id}`
- `POST /api/v1/quotes/{id}/send`
- `POST /api/v1/quotes/{id}/accept`
- `POST /api/v1/quotes/{id}/reject`
- `POST /api/v1/quotes/{id}/convert-to-project`

### Pipeline
- `GET /api/v1/pipeline/stages`
- `POST /api/v1/pipeline/stages`
- `PUT /api/v1/pipeline/stages/{id}`
- `DELETE /api/v1/pipeline/stages/{id}`

### Projects
- `GET /api/v1/projects`
- `GET /api/v1/projects/{id}`
- `POST /api/v1/projects`
- `PUT /api/v1/projects/{id}`
- `DELETE /api/v1/projects/{id}`
- `GET /api/v1/projects/{id}/overview`
- `GET /api/v1/projects/{id}/progress`
- `POST /api/v1/projects/{id}/assign-technician`
- `POST /api/v1/projects/{id}/assign-manager`
- `PATCH /api/v1/projects/{id}/progress`

### Installations
- `GET /api/v1/installations/{id}`
- `POST /api/v1/installations/{id}/start`
- `POST /api/v1/installations/{id}/complete`
- `PUT /api/v1/installations/{id}/checklist`
- `POST /api/v1/installations/{id}/photos`

### Dashboard
- `GET /api/v1/dashboard/overview`
- `GET /api/v1/dashboard/kpis`
- `GET /api/v1/dashboard/revenue`
- `GET /api/v1/dashboard/pipeline`
- `GET /api/v1/dashboard/projects`

### Technician workspace (me)
- `GET /api/v1/me/tasks`
- `GET /api/v1/me/installations`
- `GET /api/v1/me/schedule`

### Calendar
- `GET /api/v1/calendar`
- `GET /api/v1/calendar/technicians/{id}`
- `GET /api/v1/calendar/projects/{id}`

### Notifications
- `GET /api/v1/notifications`
- `POST /api/v1/notifications/read`

### Documents
- `POST /api/v1/documents/upload`
- `GET /api/v1/documents/projects/{projectId}`
- `GET /api/v1/documents/clients/{clientId}`

### Reports
- `GET /api/v1/reports/sales`
- `GET /api/v1/reports/projects`
- `GET /api/v1/reports/technicians`
- `GET /api/v1/reports/conversion`

### Settings
- `GET /api/v1/settings`
- `PUT /api/v1/settings`

## DTO reference (frontend quick guide)

All JSON endpoints use `Content-Type: application/json` except document upload (`multipart/form-data`).

### Common query DTO

- `PaginationQuery` (used by list endpoints): `page`, `pageSize`, `sortBy`, `sortOrder`, `search`

### Auth DTOs

- `LoginRequest` → `POST /api/v1/auth/login`
- `RegisterRequest` → `POST /api/v1/auth/register`
- `RefreshRequest` → `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`
- `PlatformLoginRequest` → `POST /api/v1/platform/auth/login`

### Core admin DTOs

- `CreateUserRequest`, `UpdateUserRequest`, `AssignRoleRequest` → users endpoints
- `CreateRoleRequest`, `UpdateRoleRequest`, `ReplaceRolePermissionsRequest` → roles endpoints
- `UpgradeSubscriptionRequest` → `POST /api/v1/subscriptions/upgrade`

### Platform DTOs

- `CreatePlatformSocietyRequest`, `UpdatePlatformSocietyRequest` → platform societies
- `CreateSubscriptionPlanRequest`, `UpdateSubscriptionPlanRequest` → platform plans
- `UpdatePlatformSubscriptionRequest` → platform subscription update

### CRM DTOs

- **Leads:** `CreateLeadRequest`, `UpdateLeadRequest`, `AddLeadActivityRequest`, `AssignLeadRequest`, `ConvertLeadRequest`, `AddLeadNoteRequest`
- **Clients:** `CreateClientRequest`, `UpdateClientRequest`
- **Deals:** `CreateDealRequest`, `UpdateDealRequest`
- **Quotes:** `CreateQuoteRequest`, `UpdateQuoteRequest`, `ConvertQuoteToProjectRequest`
- **Pipeline:** `CreatePipelineStageRequest`, `UpdatePipelineStageRequest`
- **Projects:** `CreateProjectRequest`, `UpdateProjectRequest`, `AssignProjectUserRequest`, `PatchProjectProgressRequest`
- **Installations:** `UpdateInstallationChecklistRequest`, `AddInstallationPhotoRequest`
- **Notifications:** `MarkNotificationsReadRequest`
- **Settings:** `UpdateSocietySettingsRequest`

### Date / time query params

- `GET /api/v1/reports/sales`: `from` and `to` (`DateOnly`)
- `GET /api/v1/calendar*`: `from`, `to` (`DateTimeOffset`) + optional `technicianId`, `projectId`
- `GET /api/v1/me/schedule`: `from`, `to` (`DateTimeOffset`)

### Multipart upload DTO

- `POST /api/v1/documents/upload` (`multipart/form-data`)
  - Fields: `file` (required), `projectId` (optional), `clientId` (optional), `type` (optional)

### Response envelope

Most endpoints return the standard wrapper:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "meta": null
}
```

## Full API input/output matrix

`Input` = path/query/body DTO. `Output` = `ApiResponse.data` payload shape.

### Health

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/health` | None | `{ status: "ok" }` |

### Auth (tenant + platform)

| Endpoint | Input | Output |
|---|---|---|
| `POST /api/v1/auth/login` | Body: `LoginRequest` | Token payload (`accessToken`, `refreshToken`, `userId`, `societyId`, role info) |
| `POST /api/v1/auth/register` | Body: `RegisterRequest` | Registered user/auth result |
| `POST /api/v1/auth/refresh` | Body: `RefreshRequest` | New token payload |
| `POST /api/v1/auth/logout` | Body: `RefreshRequest` | Logout confirmation |
| `GET /api/v1/auth/me` | Bearer token | Current user profile + society context |
| `POST /api/v1/platform/auth/login` | Body: `PlatformLoginRequest` | Platform token payload (`accessToken`, `platformUserId`, roles) |

### Users / roles / permissions / subscriptions

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/v1/users` | Query: `PaginationQuery` | Paged users list (`data` + `meta`) |
| `GET /api/v1/users/{id}` | Path: `id` | User details |
| `POST /api/v1/users` | Body: `CreateUserRequest` | Created user |
| `PUT /api/v1/users/{id}` | Path: `id`, Body: `UpdateUserRequest` | Updated user |
| `DELETE /api/v1/users/{id}` | Path: `id` | `{ deleted: true }` |
| `POST /api/v1/users/{id}/assign-role` | Path: `id`, Body: `AssignRoleRequest` | Updated user/assignment confirmation |
| `GET /api/v1/roles` | None | Roles list |
| `POST /api/v1/roles` | Body: `CreateRoleRequest` | Created role |
| `PUT /api/v1/roles/{id}` | Path: `id`, Body: `UpdateRoleRequest` | Updated role |
| `DELETE /api/v1/roles/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/roles/{id}/permissions` | Path: `id` | Role permissions list |
| `PUT /api/v1/roles/{id}/permissions` | Path: `id`, Body: `ReplaceRolePermissionsRequest` | `{ updated: true }` |
| `GET /api/v1/roles/catalog/commercial-suggestions` | None | Suggested role names list |
| `GET /api/v1/permissions` | None | Permissions list |
| `GET /api/v1/subscriptions/current` | None | Current subscription |
| `POST /api/v1/subscriptions/upgrade` | Body: `UpgradeSubscriptionRequest` | Updated subscription |

### Platform admin

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/v1/platform/societies` | None | Platform societies list |
| `GET /api/v1/platform/societies/{id}` | Path: `id` | Platform society details |
| `POST /api/v1/platform/societies` | Body: `CreatePlatformSocietyRequest` | Created platform society |
| `PUT /api/v1/platform/societies/{id}` | Path: `id`, Body: `UpdatePlatformSocietyRequest` | Updated platform society |
| `DELETE /api/v1/platform/societies/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/platform/subscription-plans` | None | Subscription plans list |
| `POST /api/v1/platform/subscription-plans` | Body: `CreateSubscriptionPlanRequest` | Created plan |
| `PUT /api/v1/platform/subscription-plans/{id}` | Path: `id`, Body: `UpdateSubscriptionPlanRequest` | Updated plan |
| `PUT /api/v1/platform/subscriptions/{id}` | Path: `id`, Body: `UpdatePlatformSubscriptionRequest` | Updated subscription row |

### CRM - leads / clients / deals / quotes

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/v1/leads` | Query: `PaginationQuery` | Paged leads list |
| `GET /api/v1/leads/{id}` | Path: `id` | Lead details |
| `POST /api/v1/leads` | Body: `CreateLeadRequest` | Created lead |
| `PUT /api/v1/leads/{id}` | Path: `id`, Body: `UpdateLeadRequest` | Updated lead |
| `DELETE /api/v1/leads/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/leads/{id}/activities` | Path: `id` | Lead activities list |
| `POST /api/v1/leads/{id}/activities` | Path: `id`, Body: `AddLeadActivityRequest` | Created lead activity |
| `POST /api/v1/leads/{id}/assign` | Path: `id`, Body: `AssignLeadRequest` | Updated lead assignment |
| `POST /api/v1/leads/{id}/convert` | Path: `id`, Body: `ConvertLeadRequest` | `ConvertLeadResultDto` (`lead`, `clientId`, `dealId`) |
| `POST /api/v1/leads/{id}/mark-won` | Path: `id` | Updated lead |
| `POST /api/v1/leads/{id}/mark-lost` | Path: `id` | Updated lead |
| `POST /api/v1/leads/{id}/notes` | Path: `id`, Body: `AddLeadNoteRequest` | Created note/activity |
| `GET /api/v1/leads/{id}/timeline` | Path: `id` | Timeline entries list |
| `GET /api/v1/clients` | Query: `PaginationQuery` | Paged clients list |
| `GET /api/v1/clients/{id}` | Path: `id` | Client details |
| `POST /api/v1/clients` | Body: `CreateClientRequest` | Created client |
| `PUT /api/v1/clients/{id}` | Path: `id`, Body: `UpdateClientRequest` | Updated client |
| `DELETE /api/v1/clients/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/deals` | Query: `PaginationQuery` | Paged deals list |
| `GET /api/v1/deals/{id}` | Path: `id` | Deal details |
| `POST /api/v1/deals` | Body: `CreateDealRequest` | Created deal |
| `PUT /api/v1/deals/{id}` | Path: `id`, Body: `UpdateDealRequest` | Updated deal |
| `DELETE /api/v1/deals/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/quotes` | Query: `PaginationQuery` | Paged quotes list |
| `GET /api/v1/quotes/{id}` | Path: `id` | Quote details |
| `POST /api/v1/quotes` | Body: `CreateQuoteRequest` | Created quote |
| `PUT /api/v1/quotes/{id}` | Path: `id`, Body: `UpdateQuoteRequest` | Updated quote |
| `DELETE /api/v1/quotes/{id}` | Path: `id` | `{ deleted: true }` |
| `POST /api/v1/quotes/{id}/send` | Path: `id` | Quote with `Sent` status |
| `POST /api/v1/quotes/{id}/accept` | Path: `id` | Quote with `Accepted` status |
| `POST /api/v1/quotes/{id}/reject` | Path: `id` | Quote with `Rejected` status |
| `POST /api/v1/quotes/{id}/convert-to-project` | Path: `id`, Body: `ConvertQuoteToProjectRequest` | Quote linked/conversion result |

### CRM - pipeline / projects / installations

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/v1/pipeline/stages` | None | Pipeline stages list |
| `POST /api/v1/pipeline/stages` | Body: `CreatePipelineStageRequest` | Created stage |
| `PUT /api/v1/pipeline/stages/{id}` | Path: `id`, Body: `UpdatePipelineStageRequest` | Updated stage |
| `DELETE /api/v1/pipeline/stages/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/projects` | Query: `PaginationQuery`, optional `clientId` | Paged projects list |
| `GET /api/v1/projects/{id}` | Path: `id` | Project details |
| `POST /api/v1/projects` | Body: `CreateProjectRequest` | Created project |
| `PUT /api/v1/projects/{id}` | Path: `id`, Body: `UpdateProjectRequest` | Updated project |
| `DELETE /api/v1/projects/{id}` | Path: `id` | `{ deleted: true }` |
| `GET /api/v1/projects/{id}/overview` | Path: `id` | Project overview DTO |
| `GET /api/v1/projects/{id}/progress` | Path: `id` | Project progress DTO |
| `POST /api/v1/projects/{id}/assign-technician` | Path: `id`, Body: `AssignProjectUserRequest` | Updated project |
| `POST /api/v1/projects/{id}/assign-manager` | Path: `id`, Body: `AssignProjectUserRequest` | Updated project |
| `PATCH /api/v1/projects/{id}/progress` | Path: `id`, Body: `PatchProjectProgressRequest` | Updated project |
| `GET /api/v1/installations/{id}` | Path: `id` | Installation details |
| `POST /api/v1/installations/{id}/start` | Path: `id` | Installation status update |
| `POST /api/v1/installations/{id}/complete` | Path: `id` | Installation status update |
| `PUT /api/v1/installations/{id}/checklist` | Path: `id`, Body: `UpdateInstallationChecklistRequest` | Checklist items list |
| `POST /api/v1/installations/{id}/photos` | Path: `id`, Body: `AddInstallationPhotoRequest` | Created photo entry |

### CRM - dashboard / me / calendar / notifications / documents / reports / settings

| Endpoint | Input | Output |
|---|---|---|
| `GET /api/v1/dashboard/overview` | None | Dashboard overview DTO |
| `GET /api/v1/dashboard/kpis` | None | KPI DTO (`leadsCount`, `conversionRate`, etc.) |
| `GET /api/v1/dashboard/revenue` | None | Revenue dataset |
| `GET /api/v1/dashboard/pipeline` | None | Pipeline analytics dataset |
| `GET /api/v1/dashboard/projects` | None | Project analytics dataset |
| `GET /api/v1/me/tasks` | None | Current user tasks list |
| `GET /api/v1/me/installations` | None | Current user installations list |
| `GET /api/v1/me/schedule` | Query: `from`, `to` (`DateTimeOffset`) | Current user schedule list |
| `GET /api/v1/calendar` | Query: `from?`, `to?`, `technicianId?`, `projectId?` | Calendar events list |
| `GET /api/v1/calendar/technicians/{id}` | Path: `id`, query `from?`, `to?` | Technician events list |
| `GET /api/v1/calendar/projects/{id}` | Path: `id`, query `from?`, `to?` | Project events list |
| `GET /api/v1/notifications` | Query: `PaginationQuery` | Paged notifications list |
| `POST /api/v1/notifications/read` | Body: `MarkNotificationsReadRequest` | `{ read: true }` |
| `POST /api/v1/documents/upload` | Multipart: `file`, `projectId?`, `clientId?`, `type?` | Registered document entry (`url`, metadata) |
| `GET /api/v1/documents/projects/{projectId}` | Path: `projectId` | Project documents list |
| `GET /api/v1/documents/clients/{clientId}` | Path: `clientId` | Client documents list |
| `GET /api/v1/reports/sales` | Query: `from?`, `to?` (`DateOnly`) | Sales report dataset |
| `GET /api/v1/reports/projects` | None | Projects report dataset |
| `GET /api/v1/reports/technicians` | None | Technicians report dataset |
| `GET /api/v1/reports/conversion` | None | Conversion report dataset |
| `GET /api/v1/settings` | None | Society settings DTO |
| `PUT /api/v1/settings` | Body: `UpdateSocietySettingsRequest` | Updated society settings DTO |
