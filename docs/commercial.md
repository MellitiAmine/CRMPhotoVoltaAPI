# Commercial Management — Backend API Documentation

> **Base URL:** `/api/v1/commercials`  
> **Auth:** Bearer JWT (`TenantJwt` scheme, `society_id` claim required)  
> **Last updated:** 2026-05-08

---

## Overview

The Commercials API manages HR profiles and performance data for commercial (sales) employees within a society (tenant). Each `CommercialProfile` is scoped to a society and linked to a tenant user account.

**Key features:**
- Full CRUD for commercial profiles (HR data)
- Structured KPI snapshot storage (activities, meetings, leads, deals, attendance)
- Server-side performance score computation (same algorithm as the frontend mock)
- Paged, filterable list with multi-criteria search
- Aggregate stats endpoint for the dashboard KPI strip

---

## Domain Entity

### `CommercialProfile` (table: `app.CommercialProfiles`)

Inherits `SocietyScopedEntity → EntityBase`.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `uuid` | PK |
| `SocietyId` | `uuid` | Tenant scope (indexed) |
| `UserId` | `uuid` | Linked tenant user (unique per society) |
| `EmployeeId` | `varchar(50)` | HR reference (e.g. EMP-2024-001, unique per society) |
| `FirstName` | `varchar(100)` | |
| `LastName` | `varchar(100)` | |
| `Email` | `varchar(200)` | |
| `Phone` | `varchar(30)` | |
| `AvatarUrl` | `text` | URL to profile picture |
| `DateOfBirth` | `text` | ISO date string |
| `Address`, `City` | `text` | |
| `EmergencyContact*` | `text` | Name, Phone, Relation |
| `Department` | `varchar(120)` | |
| `Position` | `varchar(120)` | Job title |
| `ContractType` | `varchar(30)` | CDI, CDD, Stage, Freelance, Alternance |
| `WorkTime` | `varchar(20)` | full_time, part_time |
| `Salary` | `decimal(18,2)` | Annual gross (€) |
| `Status` | `varchar(20)` | active, on_leave, suspended, terminated |
| `StartDate` | `varchar(10)` | ISO date |
| `MonthlyTarget` | `decimal(18,2)` | Monthly revenue target (€) |
| **Score** | | |
| `ScoreTotal` | `int` | 0–100 composite score |
| `ScoreTier` | `varchar(20)` | top, good, average, low |
| `ScoreTrend` | `varchar(10)` | up, stable, down |
| `ScoreTrendValue` | `int` | Delta points vs last period |
| `ScoredAt` | `timestamptz` | When score was last computed |
| `ScoreActivities` | `double` | Contribution: 0–20 |
| `ScoreMeetings` | `double` | Contribution: 0–20 |
| `ScoreLeads` | `double` | Contribution: 0–15 |
| `ScoreDeals` | `double` | Contribution: 0–25 |
| `ScoreAttendance` | `double` | Contribution: 0–15 |
| `ScorePenalties` | `double` | ≤ 0, min -15 |
| **KPIs** | | |
| `KpiActivitiesCreated` | `int` | CRM activities logged this period |
| `KpiMeetingsParticipated` | `int` | Calendar events as participant |
| `KpiLeadsAssigned` | `int` | Active leads count |
| `KpiDealsWon` | `int` | Won deals count |
| `KpiQuotesGenerated` | `int` | Quotes created |
| `KpiRevenueGenerated` | `decimal(18,2)` | Revenue from won deals (€) |
| `KpiConversionRate` | `double` | % |
| `KpiPenalties` | `int` | Disciplinary flags |
| **Attendance** | | |
| `AttendancePresentDays` | `int` | |
| `AttendanceTotalWorkingDays` | `int` | Default 22 |
| `AttendanceAbsentDays` | `int` | |
| `AttendanceLateDays` | `int` | |
| `AttendanceHoursWorked` | `double` | |
| `AttendanceExpectedHours` | `double` | Default 160 |
| `AttendancePct` | `double` | % |
| **Audit** | | EntityBase fields: CreatedAt, UpdatedAt, IsDeleted |

---

## Scoring Algorithm

```
score = clamp(0, 100,
  min(20, activitiesCreated / 40 * 20)      // Activities
  + min(20, meetingsParticipated / 15 * 20)  // Meetings
  + min(15, leadsAssigned / 30 * 15)         // Leads volume
  + min(25, dealsWon / 8 * 25)               // Deals won
  + (attendancePct / 100) * 15               // Attendance
  + max(-15, penalties * -5)                 // Penalties
)
```

Tier thresholds: **top ≥ 80 | good ≥ 65 | average ≥ 50 | low < 50**

---

## Endpoints

### `GET /api/v1/commercials`

Returns a paged, filtered list of commercial profiles ordered by score descending.

**Query parameters:**

| Param | Type | Description |
|-------|------|-------------|
| `search` | string | Full-text on name, email, employeeId, position, department |
| `status` | string | active, on_leave, suspended, terminated |
| `contractType` | string | CDI, CDD, Stage, Freelance, Alternance |
| `department` | string | Exact department name |
| `scoreTier` | string | top, good, average, low |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20, max 100 |

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "userId": "uuid",
      "employeeId": "EMP-2024-001",
      "firstName": "Amir",
      "lastName": "Moreau",
      "email": "amir.moreau@solarflow.fr",
      "phone": "0640201003",
      "department": "Commercial Nord",
      "position": "Commercial Senior",
      "contractType": "CDI",
      "workTime": "full_time",
      "status": "active",
      "startDate": "2023-06-01",
      "salary": 45000.00,
      "monthlyTarget": 50000.00,
      "score": {
        "total": 87,
        "tier": "top",
        "trend": "up",
        "trendValue": 5,
        "breakdown": {
          "activities": 18.5,
          "meetings": 20.0,
          "leads": 13.5,
          "deals": 25.0,
          "attendance": 14.2,
          "penalties": 0.0
        }
      },
      "kpis": {
        "activitiesCreated": 37,
        "meetingsParticipated": 15,
        "leadsAssigned": 27,
        "dealsWon": 8,
        "quotesGenerated": 12,
        "revenueGenerated": 180000.00,
        "conversionRate": 29.6,
        "penalties": 0
      },
      "attendance": {
        "presentDays": 21,
        "totalWorkingDays": 22,
        "absentDays": 0,
        "lateDays": 1,
        "hoursWorked": 168.5,
        "expectedHours": 160,
        "attendancePct": 94.7
      }
    }
  ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 12,
    "totalPages": 1
  }
}
```

---

### `GET /api/v1/commercials/stats`

Returns aggregate statistics.

**Response 200:**
```json
{
  "success": true,
  "data": {
    "totalCount": 12,
    "activeCount": 10,
    "onLeaveCount": 1,
    "topPerformers": 3,
    "lowPerformers": 1,
    "totalSalary": 480000.00,
    "avgScore": 68.5,
    "avgAttendancePct": 91.3,
    "avgConversionRate": 22.4,
    "totalRevenue": 1450000.00
  }
}
```

---

### `GET /api/v1/commercials/{id}`

Returns the full profile including emergency contact.

**Response 200:** Full `CommercialProfileDto` (same as list item + `avatarUrl`, `dateOfBirth`, `address`, `city`, `emergencyContact`).

---

### `POST /api/v1/commercials`

Creates a new commercial profile.

**Request body:**
```json
{
  "userId": "uuid",
  "employeeId": "EMP-2024-013",
  "firstName": "Marie",
  "lastName": "Dupont",
  "email": "marie.dupont@solarflow.fr",
  "phone": "0655443322",
  "dateOfBirth": "1990-03-15",
  "address": "12 Rue Victor Hugo",
  "city": "Paris",
  "department": "Commercial Sud",
  "position": "Commercial",
  "contractType": "CDI",
  "workTime": "full_time",
  "salary": 38000.00,
  "startDate": "2026-06-01",
  "monthlyTarget": 45000.00,
  "emergencyContactName": "Jean Dupont",
  "emergencyContactPhone": "0612345678",
  "emergencyContactRelation": "Conjoint(e)"
}
```

**Response 201:** Full `CommercialProfileDto` with `ScoreTotal = 0` (awaiting first KPI push).

---

### `PUT /api/v1/commercials/{id}`

Updates HR fields. All fields are optional (PATCH semantics via nullable values).

**Request body:**
```json
{
  "status": "on_leave",
  "department": "Commercial Ouest",
  "salary": 42000.00
}
```

---

### `PATCH /api/v1/commercials/{id}/kpis`

Pushes a fresh KPI snapshot and **triggers server-side score recomputation**.

Call this after:
- A nightly sync job aggregates the month's CRM data
- A deal is marked won/lost (real-time update)
- An admin manually adjusts a KPI

**Request body:**
```json
{
  "activitiesCreated": 37,
  "meetingsParticipated": 12,
  "leadsAssigned": 22,
  "dealsWon": 6,
  "quotesGenerated": 9,
  "revenueGenerated": 140000.00,
  "conversionRate": 27.3,
  "penalties": 0
}
```

**Response 200:** Updated `CommercialProfileDto` with recalculated `score`.

---

### `DELETE /api/v1/commercials/{id}`

Soft-deletes the commercial profile (sets `IsDeleted = true`). The linked user account is not affected.

**Response 200:**
```json
{ "success": true, "data": { "deleted": true } }
```

---

## Indexes

```sql
-- Primary lookup
CREATE INDEX IX_CommercialProfiles_SocietyId ON app."CommercialProfiles" ("SocietyId");

-- Unique user per society (soft-delete aware)
CREATE UNIQUE INDEX IX_CommercialProfiles_SocietyId_UserId
  ON app."CommercialProfiles" ("SocietyId", "UserId")
  WHERE "IsDeleted" = false;

-- Unique employee ID per society (soft-delete aware)
CREATE UNIQUE INDEX IX_CommercialProfiles_SocietyId_EmployeeId
  ON app."CommercialProfiles" ("SocietyId", "EmployeeId")
  WHERE "IsDeleted" = false;
```

---

## Migration

Run the EF Core migration:

```bash
dotnet ef database update --context AppDbContext
```

This applies `20260508100000_AddCommercialProfiles` which creates the `app.CommercialProfiles` table and all indexes.

---

## Service Registration

`ICommercialService` is registered as **scoped** in `DependencyInjection.cs`:

```csharp
services.AddScoped<ICommercialService, CommercialService>();
```

---

## Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `NOT_FOUND` | 404 | Commercial profile not found for this society |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT / no user claim |
| `TENANT_REQUIRED` | 403 | No `society_id` claim in token |
| `VALIDATION_ERROR` | 422 | Invalid request body fields |

---

## Integration with Other Services

| Integration | How |
|-------------|-----|
| KPI sync from leads | After `POST /leads/{id}/mark-won`, call `PATCH /commercials/{profileId}/kpis` |
| Attendance from HR | External HR system pushes attendance via `PUT /commercials/{id}` |
| Calendar events | Query `GET /calendar?from=...&to=...` filtering by `participants` containing the userId |
| Leads | Query `GET /leads?assignedToUserId={userId}` |

---

## Future Enhancements

- `GET /api/v1/commercials/{id}/timeline` — audit trail of score changes
- `GET /api/v1/commercials/{id}/leads` — assigned leads (proxied from LeadsController)
- `GET /api/v1/commercials/{id}/events` — calendar events (proxied from CalendarController)
- `POST /api/v1/commercials/{id}/penalties` — add a penalty with reason
- Webhook / SignalR push when score tier changes
- Monthly score archival table for trend analysis
