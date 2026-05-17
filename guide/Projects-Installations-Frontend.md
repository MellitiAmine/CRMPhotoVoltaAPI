# Projects & Installations — Frontend API Guide

Complete reference for Angular integration. Base URL: `/api/v1`. Auth: `Authorization: Bearer {token}`.

---

## Projects — List & detail fields

### `GET /api/v1/projects` (paginated)

Each item (`ProjectListItemDto`) includes:

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | |
| `reference` | string? | e.g. `PRJ-2026-0012` |
| `name` | string | |
| `clientId` | Guid | |
| `clientName` | string | |
| `leadId` | Guid? | |
| `status` | enum | `New`, `Study`, `Installation`, … |
| `priority` | enum | `Low`, `Medium`, `High`, `Urgent` |
| `totalTtc` | decimal | Financial total (from quote / project) |
| `commercialUserId` | Guid? | |
| `commercialName` | string? | Resolved display name |
| `systemSizeKw` | decimal? | |
| `progressPercent` | int | 0–100 |
| `expectedInstallationDate` | date? | |
| `lastActivityAt` | datetime? | |
| `createdAt` | datetime | |

**Query:** `?page=1&pageSize=20&search=PRJ&sortBy=reference&sortOrder=desc&clientId={guid}`

**Sort fields:** `createdAt`, `name`, `reference`, `status`, `priority`, `totalTtc`, `client`

### `GET /api/v1/projects/{id}`

Full `ProjectDto` adds: `totalHt`, `totalTva`, `notes`, `managerUserId`, `managerName`, `technicianUserId`, `technicianName`, `quoteId`, `address`, etc.

### `GET /api/v1/projects/{id}/detail`

Full aggregate: client, lead, quote, tasks, timeline, documents, contracts, invoices, financial summary.

---

## Projects — Mutations

| Method | Endpoint | Body |
|--------|----------|------|
| POST | `/projects` | `CreateProjectRequest` |
| PUT | `/projects/{id}` | `UpdateProjectRequest` |
| POST | `/projects/{id}/change-status` | `{ status, comment? }` |
| POST | `/projects/{id}/assign-commercial` | `{ userId }` |
| POST | `/projects/{id}/assign-manager` | `{ userId }` |
| POST | `/projects/{id}/assign-technician` | `{ userId }` |

---

## Installations

### `GET /api/v1/installations`

Paginated list. Filters: `?projectId=`, `?technicianId=`, `search=`

`InstallationListItemDto`:

| Field | Description |
|-------|-------------|
| `projectReference` | Project ref |
| `projectName` | |
| `clientName` | |
| `technicianName` | |
| `date` | Planned date |
| `status` | `Scheduled`, `InProgress`, `Completed`, `Cancelled` |
| `checklistCompleted` / `checklistTotal` | Progress |

### `GET /api/v1/projects/{id}/installations`

All installations for one project.

### `POST /api/v1/installations`

```json
{
  "projectId": "...",
  "technicianId": "...",
  "date": "2026-06-20"
}
```

Creates default checklist (6 items) + `InstallationPlanned` timeline event.

### Workflow

| Method | Endpoint | Effect |
|--------|----------|--------|
| POST | `/installations/{id}/start` | Status → InProgress, timeline |
| POST | `/installations/{id}/complete` | Status → Completed, project → Activated (if applicable) |
| PUT | `/installations/{id}/checklist` | Update checklist items |
| POST | `/installations/{id}/photos` | `{ url }` |

---

## Recommended Angular screens

1. **Projects list** — columns: Référence, Client, Statut, Priorité, Total TTC, Commercial, Progression
2. **Project detail** — tabs: Overview, Timeline, Tasks, Installations, Documents, Finance
3. **Installations list** — filter by technician / project
4. **Installation detail** — checklist + photos + start/complete actions

---

## Error codes (projects / installations)

| Code | HTTP |
|------|------|
| `PROJECT_NOT_FOUND` | 404 |
| `INSTALLATION_NOT_FOUND` | 404 |
| `INVALID_TRANSITION` | 400 |
| `USER_NOT_IN_SOCIETY` | 400 |
| `VALIDATION_ERROR` | 400 |
