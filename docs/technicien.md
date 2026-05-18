# Technicien Management — Backend API Documentation

> **Base URL:** `/api/v1/techniciens`  
> **Auth:** Bearer JWT (`TenantJwt` scheme, `society_id` claim required)  
> **Last updated:** 2026-05-18

---

## Overview

The Techniciens API manages HR profiles and performance data for field technicians within a society (tenant). Each `TechnicienProfile` is scoped to a society and linked to a tenant user account in `core.Users` via `UserId`.

**Key features:**
- Full CRUD for technicien profiles (HR data)
- Structured KPI snapshot storage (interventions, site visits, projects, installations)
- Server-side performance score computation
- Paged, filterable list with multi-criteria search
- Aggregate stats endpoint for the dashboard KPI strip
- Calendar events for a technicien's linked user

On **create**, the API:
1. Resolves or creates a `core.Users` row (by `userId` or email)
2. Ensures `core.UserSocieties` membership with role **Technicien** (`RoleType.Technicien = 4`)
3. Inserts `app.TechnicienProfiles` (one active profile per user per society)

**Delete** is a soft-delete (`IsDeleted = true`); the linked user account is not removed.

---

## Domain Entity

### `TechnicienProfile` (table: `app.TechnicienProfiles`)

Inherits `SocietyScopedEntity → EntityBase`.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `uuid` | PK |
| `SocietyId` | `uuid` | Tenant scope (indexed) |
| `UserId` | `uuid` | Linked tenant user (`core.Users.Id`, unique per society) |
| `EmployeeId` | `varchar(50)` | HR reference (e.g. TECH-2026-001, unique per society) |
| `FirstName`, `LastName`, `Email`, `Phone` | | Personal info |
| `AvatarUrl`, `DateOfBirth`, `Address`, `City` | | |
| `EmergencyContact*` | `text` | Name, Phone, Relation |
| `Department`, `Position` | `varchar(120)` | |
| `ContractType` | `varchar(30)` | CDI, CDD, Stage, Freelance, Alternance |
| `WorkTime` | `varchar(20)` | full_time, part_time |
| `Salary` | `decimal(18,2)` | Annual gross (€) |
| `Status` | `varchar(20)` | active, on_leave, suspended, terminated |
| `StartDate` | `varchar(10)` | ISO date |
| `MonthlyTarget` | `int` | Monthly installation/intervention target count |
| **Score** | | |
| `ScoreTotal` | `int` | 0–100 composite score |
| `ScoreTier` | `varchar(20)` | top, good, average, low |
| `ScoreTrend` | `varchar(10)` | up, stable, down |
| `ScoreTrendValue` | `int` | Delta vs last period |
| `ScoredAt` | `timestamptz` | Last score computation |
| `ScoreInterventions` | `double` | 0–20 |
| `ScoreSiteVisits` | `double` | 0–20 |
| `ScoreProjects` | `double` | 0–15 |
| `ScoreInstallations` | `double` | 0–25 |
| `ScoreAttendance` | `double` | 0–15 |
| `ScorePenalties` | `double` | ≤ 0, min -15 |
| **KPIs** | | |
| `KpiInterventionsCompleted` | `int` | |
| `KpiSiteVisitsCompleted` | `int` | |
| `KpiProjectsAssigned` | `int` | |
| `KpiInstallationsCompleted` | `int` | |
| `KpiReportsSubmitted` | `int` | |
| `KpiHoursOnSite` | `double` | |
| `KpiOnTimeRate` | `double` | % on-time deliveries |
| `KpiPenalties` | `int` | |
| **Attendance** | | Same structure as commercial profiles |
| **Audit** | | CreatedAt, UpdatedAt, IsDeleted |

---

## Scoring Algorithm

```
score = clamp(0, 100,
  min(20, interventionsCompleted / 40 * 20)
  + min(20, siteVisitsCompleted / 15 * 20)
  + min(15, projectsAssigned / 30 * 15)
  + min(25, installationsCompleted / 8 * 25)
  + (attendancePct / 100) * 15
  + max(-15, penalties * -5)
)
```

Tier thresholds: **top ≥ 80 | good ≥ 65 | average ≥ 50 | low < 50**

---

## Endpoints

### `GET /api/v1/techniciens`

Paged list ordered by score descending.

| Param | Type | Description |
|-------|------|-------------|
| `search` | string | Name, email, employeeId, position, department |
| `status` | string | active, on_leave, suspended, terminated |
| `contractType` | string | CDI, CDD, Stage, Freelance, Alternance |
| `department` | string | Exact match |
| `scoreTier` | string | top, good, average, low |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20, max 100 |

**Response 200:** Paged `TechnicienListItemDto[]` with `meta` (page, pageSize, totalCount, totalPages).

---

### `GET /api/v1/techniciens/stats`

Aggregate statistics for the dashboard strip.

**Response 200:**
```json
{
  "success": true,
  "data": {
    "totalCount": 8,
    "activeCount": 7,
    "onLeaveCount": 0,
    "topPerformers": 2,
    "lowPerformers": 1,
    "totalSalary": 320000.00,
    "avgScore": 72.1,
    "avgAttendancePct": 93.5,
    "avgOnTimeRate": 88.2,
    "totalHoursOnSite": 1240.5
  }
}
```

---

### `GET /api/v1/techniciens/{id}`

Full `TechnicienProfileDto` including emergency contact and attendance.

---

### `GET /api/v1/techniciens/{id}/calendar-events`

Calendar events where the technicien is participant or assignee (`from`, `to` optional query params). Returns `[]` if `userId` is null (user no longer in society).

---

### `POST /api/v1/techniciens`

Creates a technicien profile and links/creates the user account.

**Request body:**
```json
{
  "userId": null,
  "employeeId": null,
  "firstName": "Lucas",
  "lastName": "Bernard",
  "email": "lucas.bernard@solarflow.fr",
  "phone": "0611223344",
  "dateOfBirth": "1988-11-20",
  "address": "5 Rue des Artisans",
  "city": "Lyon",
  "department": "Technique Sud",
  "position": "Technicien poseur",
  "contractType": "CDI",
  "workTime": "full_time",
  "salary": 36000.00,
  "startDate": "2026-01-15",
  "monthlyTarget": 12,
  "emergencyContactName": "Sophie Bernard",
  "emergencyContactPhone": "0699887766",
  "emergencyContactRelation": "Conjoint(e)"
}
```

- `userId`: optional — link existing user; if omitted, find by email or create user
- `employeeId`: optional — auto-generated as `TECH-{year}-{seq}` if omitted

**Response 201:** Full `TechnicienProfileDto` with `score.total = 0`.

**Errors:**
| Code | HTTP | Description |
|------|------|-------------|
| `TECHNICIEN_USER_ALREADY_EXISTS` | 409 | User already has a profile in this society |

---

### `PUT /api/v1/techniciens/{id}`

Updates HR fields (partial update — all body fields optional).

```json
{
  "status": "on_leave",
  "department": "Technique Ouest",
  "monthlyTarget": 15
}
```

---

### `PATCH /api/v1/techniciens/{id}/kpis`

Pushes KPI snapshot and **recomputes score**.

```json
{
  "interventionsCompleted": 28,
  "siteVisitsCompleted": 14,
  "projectsAssigned": 18,
  "installationsCompleted": 7,
  "reportsSubmitted": 22,
  "hoursOnSite": 156.5,
  "onTimeRate": 91.2,
  "penalties": 0
}
```

---

### `DELETE /api/v1/techniciens/{id}`

Soft-deletes the profile (suppression logique).

**Response 200:**
```json
{ "success": true, "data": { "deleted": true } }
```

---

## Indexes

```sql
CREATE INDEX IX_TechnicienProfiles_SocietyId ON app."TechnicienProfiles" ("SocietyId");

CREATE UNIQUE INDEX IX_TechnicienProfiles_SocietyId_UserId
  ON app."TechnicienProfiles" ("SocietyId", "UserId")
  WHERE "IsDeleted" = false;

CREATE UNIQUE INDEX IX_TechnicienProfiles_SocietyId_EmployeeId
  ON app."TechnicienProfiles" ("SocietyId", "EmployeeId")
  WHERE "IsDeleted" = false;
```

---

## Migration

```bash
dotnet ef database update --context AppDbContext
```

Applies `20260518100000_AddTechnicienProfiles`.

---

## Service Registration

```csharp
services.AddScoped<ITechnicienService, TechnicienService>();
```

---

## Authorization

Role policy `society.technician` accepts society roles: **Admin**, **Technicien**, or **Technician**.

New techniciens created via the API receive the **Technicien** role (`RoleType.Technicien = 4`) in `core.Roles` / `core.UserSocieties`.

---

## Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `NOT_FOUND` | 404 | Profile not found for this society |
| `TECHNICIEN_USER_ALREADY_EXISTS` | 409 | Duplicate user link |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `TENANT_REQUIRED` | 403 | No `society_id` claim |
| `VALIDATION_ERROR` | 422 | Invalid request body |

---

## Integration with Other Services

| Integration | How |
|-------------|-----|
| Projects | `POST /api/v1/projects/{id}/assign-technician` sets `Project.TechnicianUserId` |
| Installations | `Installation.TechnicianId` references the same `UserId` |
| Calendar | `GET /api/v1/techniciens/{id}/calendar-events` |
| KPI sync | After installation completion, call `PATCH /techniciens/{id}/kpis` |

---

## Parity with Commercials API

| Commercial | Technicien |
|------------|------------|
| `GET /commercials` | `GET /techniciens` |
| `GET /commercials/stats` | `GET /techniciens/stats` |
| `GET /commercials/{id}` | `GET /techniciens/{id}` |
| `GET /commercials/{id}/calendar-events` | `GET /techniciens/{id}/calendar-events` |
| `POST /commercials` | `POST /techniciens` |
| `PUT /commercials/{id}` | `PUT /techniciens/{id}` |
| `PATCH /commercials/{id}/kpis` | `PATCH /techniciens/{id}/kpis` |
| `DELETE /commercials/{id}` | `DELETE /techniciens/{id}` |
