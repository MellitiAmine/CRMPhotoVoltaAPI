# Tenant CRM guide (full workflow)

Guide for users **inside a society** (admin, commercial, technician). Routes are under `/api/v1/...` **except** `/api/v1/platform/...`.

Use a **tenant JWT** from `POST /api/v1/auth/login`. The token embeds **`society_id`**; the API resolves tenant context from that claim (no separate `society_id` HTTP header). Send:

`Authorization: Bearer <tenant_access_token>`

---

## Prerequisites

- API running; optional: `GET /api/health` → `{ "status": "ok" }`.
- Demo accounts (passwords from `appsettings` / `ACCOUNTS.md`), e.g.:
  - `admin.essai@crm.local` / `Demo123!` — free-trial demo society  
  - `admin.payant@crm.local` / `Demo123!` — paid demo society  

---

## 1) Tenant login

`POST /api/v1/auth/login`

```json
{
  "email": "admin.payant@crm.local",
  "password": "Demo123!"
}
```

From `data`, keep:

- **`accessToken`** — use on every tenant call  
- **`societyId`** — current society embedded in the token (same as JWT `society_id` claim)  
- **`refreshToken`** — for renewal  

Other auth endpoints (no tenant CRM context required on anonymous routes):

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/v1/auth/register` | Register (if enabled by product) |
| `POST` | `/api/v1/auth/refresh` | New tokens from refresh token |
| `POST` | `/api/v1/auth/logout` | Invalidate refresh session |

---

## 2) Session: who am I

### 2.1 Current user

`GET /api/v1/auth/me`  

Header: `Authorization: Bearer <tenant_access_token>`

Expect profile and the **current** society context (`currentSocietyId`, `societies` array — should be **one** membership per account).

If legacy data still has several memberships for the same user, this call returns **`403`** `MULTI_SOCIETY_NOT_ALLOWED` until the data is cleaned.

There is **no** `POST /auth/switch-society` route: one account = one organization. New societies are created only via **`/api/v1/platform/societies`** (platform JWT). If you need another org, use another tenant account or ask the platform operator.

---

## 3) Tenant subscription (billing view)

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/v1/subscriptions/current` | Current plan window for this society |
| `POST` | `/api/v1/subscriptions/upgrade` | Request upgrade (validated server-side) |

---

## 4) Workflow — Society admin (users, roles, CRM settings)

Requires **Admin** policy on routes that enforce it (`/users`, `/roles`, `/permissions`).

Societies are **not** managed from the tenant API: create/update/delete via **`/api/v1/platform/societies`** (platform JWT). Tenants see the current org in **`GET /api/v1/auth/me`**.

| Area | Typical flow |
|------|----------------|
| **Users** | `GET /api/v1/users` → `POST /api/v1/users` → `POST /api/v1/users/{id}/assign-role` |
| **Roles** | `GET /api/v1/roles` → `POST /api/v1/roles` → `PUT /api/v1/roles/{id}/permissions` |
| **Permission catalog** | `GET /api/v1/permissions` |

Role name ideas (non-binding): `GET /api/v1/roles/catalog/commercial-suggestions`

---

## 5) Workflow — Sales pipeline (lead → client → deal → quote → project)

### 5.1 Leads

| Step | Method | Route |
|------|--------|--------|
| List / create | `GET` `POST` | `/api/v1/leads` |
| Detail / update / delete | `GET` `PUT` `DELETE` | `/api/v1/leads/{id}` |
| Activities | `GET` `POST` | `/api/v1/leads/{id}/activities` |
| Assign | `POST` | `/api/v1/leads/{id}/assign` |
| Convert to client (+ optional deal) | `POST` | `/api/v1/leads/{id}/convert` |
| Outcomes | `POST` | `/api/v1/leads/{id}/mark-won`, `.../mark-lost` |
| Note / timeline | `POST` `GET` | `/api/v1/leads/{id}/notes`, `.../timeline` |

### 5.2 Clients & deals

| Resource | Routes |
|----------|--------|
| Clients | `GET` `POST` `/api/v1/clients`, `GET` `PUT` `DELETE` `/api/v1/clients/{id}` |
| Deals | `GET` `POST` `/api/v1/deals`, `GET` `PUT` `DELETE` `/api/v1/deals/{id}` |

### 5.3 Pipeline stages (deal board labels)

`GET` `POST` `/api/v1/pipeline/stages`  
`PUT` `DELETE` `/api/v1/pipeline/stages/{id}`

### 5.4 Quotes (state machine)

| Step | Route |
|------|--------|
| CRUD | `GET` `POST` `/api/v1/quotes`, `GET` `PUT` `DELETE` `/api/v1/quotes/{id}` |
| Send | `POST` `/api/v1/quotes/{id}/send` |
| Accept / reject | `POST` `/api/v1/quotes/{id}/accept`, `.../reject` |
| Create project from accepted quote | `POST` `/api/v1/quotes/{id}/convert-to-project` |

**Statuses** are enums in JSON, e.g. `Draft`, `Sent`, `Accepted`, `Rejected`, `Converted` (see `QuoteStatus` in domain).

### 5.5 Projects

| Step | Route |
|------|--------|
| CRUD | `GET` `POST` `/api/v1/projects`, `GET` `PUT` `DELETE` `/api/v1/projects/{id}` |
| Overview / progress | `GET` `/api/v1/projects/{id}/overview`, `.../progress` |
| Assign | `POST` `.../assign-technician`, `.../assign-manager` |
| Progress % | `PATCH` `.../progress` |

**Project `status`** values include `Planned`, `InProgress`, `Done`, `Cancelled` (`ProjectStatus`).

List supports filters/pagination (see `PaginationQuery` / controller parameters): e.g. `page`, `pageSize`, `search`, `sortBy`, `sortOrder`; projects list may support `clientId`.

---

## 6) Workflow — Field / technician

| Purpose | Route |
|---------|--------|
| My tasks | `GET /api/v1/me/tasks` |
| My installations | `GET /api/v1/me/installations` |
| My schedule | `GET /api/v1/me/schedule?from=...&to=...` (ISO datetimes) |
| Installation detail | `GET` `/api/v1/installations/{id}` |
| Start / complete job | `POST` `/api/v1/installations/{id}/start`, `.../complete` |
| Checklist / photos | `PUT` `.../checklist`, `POST` `.../photos` |

Calendar (planner):  
`GET /api/v1/calendar?from=&to=&technicianId=&projectId=`  
plus `GET /api/v1/calendar/technicians/{id}`, `GET /api/v1/calendar/projects/{id}`.

---

## 7) Workflow — Dashboard & reports

**Dashboard**

- `GET /api/v1/dashboard/overview`  
- `GET /api/v1/dashboard/kpis`  
- `GET /api/v1/dashboard/revenue`  
- `GET /api/v1/dashboard/pipeline`  
- `GET /api/v1/dashboard/projects`  

**Reports**

- `GET /api/v1/reports/sales`  
- `GET /api/v1/reports/projects`  
- `GET /api/v1/reports/technicians`  
- `GET /api/v1/reports/conversion`  

---

## 8) Documents, notifications, settings

| Area | Routes |
|------|--------|
| Documents | `POST /api/v1/documents/upload` (multipart), `GET .../documents/projects/{projectId}`, `GET .../clients/{clientId}` — public URLs under `/uploads/...` |
| Notifications | `GET /api/v1/notifications`, `POST /api/v1/notifications/read` |
| Society settings | `GET` `PUT` `/api/v1/settings` |

---

## 9) Isolation & errors

- Data is scoped by **`society_id` in the JWT**. Wrong or missing claim → **`403`** with `TENANT_REQUIRED` / similar.  
- Cross-society IDs → **`404`** or **`403`** (no data leakage).  
- Role policies (Admin / Commercial / Technician) → **`403`** if the role is not allowed.  
- Use **tenant** token only; **platform** token on these routes → **401**.

---

## 10) End-to-end order (suggested manual test)

1. `POST /api/v1/auth/login` → Bearer token  
2. `GET /api/v1/auth/me`  
3. `GET /api/v1/dashboard/overview`  
4. `POST /api/v1/leads` → `POST /api/v1/leads/{id}/convert`  
5. `POST /api/v1/deals` (optional)  
6. `POST /api/v1/quotes` → `POST .../send` → `.../accept` → `.../convert-to-project`  
7. `GET /api/v1/projects/{id}/overview`  
8. `GET /api/v1/reports/sales`  

Adjust steps if your seed data already contains leads/quotes.

---

## 11) Tenant route cheat sheet

| Prefix | Scope |
|--------|--------|
| `/api/v1/auth/*` | Login, refresh, me |
| `/api/v1/users`, `/roles`, `/permissions` | Admin / membership |
| `/api/v1/leads`, `/clients`, `/deals`, `/pipeline` | CRM core |
| `/api/v1/quotes` | Quotes |
| `/api/v1/projects`, `/installations` | Delivery |
| `/api/v1/dashboard`, `/reports` | Analytics |
| `/api/v1/me`, `/calendar`, `/notifications`, `/documents`, `/settings` | Ops & UX |

Full single-file list: `ACCOUNTS.md` (section “Full API documentation”).
