# Calendar API — Backend Documentation

> **Stack:** ASP.NET Core 8 · EF Core · PostgreSQL · JWT (tenant scheme)

---

## Overview

The Calendar module lets **Admin** and **Commercial** users schedule meetings, reminders, and activities inside their society. Events are scoped to a society (multi-tenant), and access is role-filtered at both the API and the database layer.

---

## Domain Model

### `CalendarEvent` entity (`CrmPhotoVolta.Domain.App.Event.cs`)

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `SocietyId` | `Guid` | Multi-tenant scope |
| `Title` | `string` (max 300) | Event title (required) |
| `Type` | `string` (max 40) | `"meeting"` \| `"reminder"` \| `"activity"` |
| `StartDate` | `DateTimeOffset` | UTC start |
| `EndDate` | `DateTimeOffset` | UTC end (must be > StartDate) |
| `Description` | `string?` (max 2000) | Optional notes |
| `AssignedToUserId` | `Guid?` | Primary assignee (first participant) |
| `Participants` | `List<Guid>` | All invited user IDs (stored as JSON in `text` column) |
| `CreatedById` | `Guid?` | User who created the event (from `EntityBase`) |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp |
| `IsDeleted` | `bool` | Soft-delete flag |

---

## Database Migration

After pulling this code, apply the migration to add the new columns:

```bash
# From the solution root
dotnet ef database update \
  --project CrmPhotoVolta.Infrastructure \
  --startup-project CRMPhotoVoltaApis \
  --context AppDbContext
```

The migration `20260506120000_CalendarEventParticipants` adds:

| Column | Type | Default |
|---|---|---|
| `Description` | `varchar(2000)` | `NULL` |
| `Participants` | `text` (JSON array of GUIDs) | `'[]'` |

---

## API Endpoints

All endpoints require `Authorization: Bearer <tenant_access_token>`.

### `GET /api/v1/calendar`

List events visible to the authenticated user.

**Access:** All authenticated users (Admin + Commercial + Technician)

**Query params:**

| Param | Type | Description |
|---|---|---|
| `from` | ISO 8601 | Filter events starting on or after this date |
| `to` | ISO 8601 | Filter events starting on or before this date |
| `technicianId` | `Guid` | Filter by assigned technician |
| `projectId` | `Guid` | Filter by project name hint |

**Role-based filtering:**
- **Admin** → all society events
- **Commercial / Technician** → only events where `CreatedById == userId` OR `userId ∈ Participants`

**Response `200 OK`:**

```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Q2 kickoff meeting",
      "type": "meeting",
      "startDate": "2026-06-01T09:00:00+00:00",
      "endDate": "2026-06-01T10:30:00+00:00",
      "description": "Quarterly planning session",
      "assignedToUserId": "...",
      "participants": ["<uuid1>", "<uuid2>"],
      "createdById": "<uuid>"
    }
  ]
}
```

---

### `POST /api/v1/calendar`

Create a new calendar event.

**Access:** `society.commercial` policy (Admin OR Commercial)

**Request body:**

```json
{
  "title": "Client onboarding",
  "type": "meeting",
  "startDate": "2026-06-15T10:00:00Z",
  "endDate": "2026-06-15T11:00:00Z",
  "description": "Onboarding session with new client",
  "participants": ["<user-guid-1>", "<user-guid-2>"]
}
```

**Validation rules:**
- `title` — required, non-empty
- `type` — must be `"meeting"`, `"reminder"`, or `"activity"`
- `endDate` must be after `startDate`
- All `participants` must belong to the same society

**Response `201 Created`:** Same shape as a single event DTO.

**Errors:**

| Code | HTTP | Meaning |
|---|---|---|
| `TITLE_REQUIRED` | 400 | Title is blank |
| `INVALID_DATE_RANGE` | 400 | End ≤ Start |
| `INVALID_EVENT_TYPE` | 400 | Type not in allowed set |
| `INVALID_PARTICIPANT` | 400 | Participant not in society |
| `UNAUTHORIZED` | 401 | Token missing/invalid |

---

### `DELETE /api/v1/calendar/{id}`

Soft-delete an event.

**Access:** `society.commercial` policy (Admin OR Commercial)

**Authorization logic:**
- Admin can delete any event
- Commercial can only delete events they created (`CreatedById == userId`)

**Response `200 OK`:**

```json
{ "success": true, "data": { "deleted": true } }
```

**Errors:**

| Code | HTTP | Meaning |
|---|---|---|
| `EVENT_NOT_FOUND` | 404 | Event doesn't exist or wrong society |
| `FORBIDDEN` | 403 | Not the creator and not admin |

---

### `GET /api/v1/calendar/technicians/{id}`

Same as `GET /api/v1/calendar` but pre-filtered by `AssignedToUserId`.

### `GET /api/v1/calendar/projects/{id}`

Same as `GET /api/v1/calendar` but pre-filtered by project name hint.

---

## Architecture

```
CalendarController
 ├── ICalendarQueryService  → CalendarQueryService  (read, role-filtered)
 └── ICalendarCommandService → CalendarCommandService (create, delete)
```

### Service registration (`DependencyInjection.cs`)

```csharp
services.AddScoped<ICalendarQueryService, CalendarQueryService>();
services.AddScoped<ICalendarCommandService, CalendarCommandService>();
```

### `CalendarQueryService`

- Queries `AppDbContext.Events` filtered by `SocietyId`
- For non-admin callers: loads events into memory and filters by `CreatedById == uid || Participants.Contains(uid)` (EF cannot serialize `List<Guid>` to SQL `LIKE` for JSON arrays)

### `CalendarCommandService`

- `CreateAsync` — validates input, checks participant society membership via `CoreDbContext.UserSocieties`, saves the new entity
- `DeleteAsync` — soft-deletes; enforces creator-or-admin rule

### `Participants` column storage

Stored as a JSON text column using EF Core's `ValueConverter<List<Guid>, string>`:

```csharp
var participantsConverter = new ValueConverter<List<Guid>, string>(
    v => JsonSerializer.Serialize(v, ...),
    v => JsonSerializer.Deserialize<List<Guid>>(v, ...) ?? new List<Guid>()
);
```

---

## Security

- JWT Bearer (tenant scheme) required on all endpoints
- Role check via `SocietyRoleRequirement` + `SocietyRoleHandler` (database lookup)
- `SocietyPolicies.Commercial` = Admin OR Commercial (both may create/delete)
- Soft-delete prevents data loss; events remain in DB with `IsDeleted = true`
- Participants validated against `CoreDbContext.UserSocieties` to prevent cross-society data leakage

---

## Conflict Detection (future)

To add overlapping meeting warnings, extend `CreateAsync` in `CalendarCommandService`:

```csharp
var overlapping = await _app.Events
    .Where(x => x.SocietyId == societyId
             && x.Participants.Any(p => request.Participants.Contains(p))
             && x.StartDate < request.EndDate
             && x.EndDate > request.StartDate)
    .AnyAsync(cancellationToken);

if (overlapping)
    throw new AppException("CONFLICT", "One or more participants have an overlapping event.", 409);
```
