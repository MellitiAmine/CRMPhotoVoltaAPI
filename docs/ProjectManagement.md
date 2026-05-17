# Project Management Module

## Overview

The Project Management module is the core of the CRM/ERP system. A **Project** represents an active photovoltaic installation engagement with a client. Projects are created automatically when a lead is marked as **Won**, or manually by admins and managers.

---

## Project Lifecycle

### Status Flow

```
New → Study → TechnicalVisit → QuoteSent → Negotiation → Approved
    → Planning → Installation → WaitingSTEG → Activated → Completed
                                                        ↘ SAV
Any state → Cancelled
```

### Status Descriptions

| Status | Description | Auto Progress % |
|--------|-------------|-----------------|
| `New` | Project just created | 5% |
| `Study` | Feasibility study in progress | 10% |
| `TechnicalVisit` | On-site technical visit planned | 20% |
| `QuoteSent` | Quote sent to client | 30% |
| `Negotiation` | Quote under negotiation | 40% |
| `Approved` | Client approved the quote | 50% |
| `Planning` | Scheduling installation team & materials | 60% |
| `Installation` | Installation in progress | 75% |
| `WaitingSTEG` | Awaiting STEG grid connection | 80% |
| `Activated` | System activated and operational | 90% |
| `Completed` | Project fully completed | 100% |
| `SAV` | After-sales service in progress | 95% |
| `Cancelled` | Project cancelled | 0% |

### Transition Rules

Each status change:
1. Validates the transition is allowed (see `ProjectWorkflowService`)
2. Updates `ProgressPercent` automatically
3. Creates a `ProjectTimelineEvent` of type `StatusChanged`
4. Sends notification to the assigned commercial

**Allowed transitions:**
- `New` → Study, Cancelled
- `Study` → TechnicalVisit, Cancelled
- `TechnicalVisit` → QuoteSent, Negotiation, Cancelled
- `QuoteSent` → Negotiation, Approved, Cancelled
- `Negotiation` → Approved, Cancelled
- `Approved` → Planning, Cancelled
- `Planning` → Installation, Cancelled
- `Installation` → WaitingSTEG, Activated, Cancelled
- `WaitingSTEG` → Activated, Cancelled
- `Activated` → Completed, SAV
- `Completed` → SAV
- `SAV` → Completed, Cancelled

---

## Project Entity

### Key Fields

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `SocietyId` | Guid | Tenant isolation |
| `LeadId` | Guid? | Source lead (nullable) |
| `ClientId` | Guid | Associated client |
| `QuoteId` | Guid? | Accepted quote |
| `DealId` | Guid? | Optional deal |
| `Reference` | string | Auto-generated `PRJ-{year}-####` |
| `Name` | string | Project name |
| `Status` | ProjectStatus | Current lifecycle status |
| `Priority` | LeadPriority | Low / Medium / High / Urgent |
| `ProgressPercent` | int | 0–100 |
| `SystemSizeKw` | decimal? | Installed power (kWc) |
| `EstimatedProduction` | decimal? | Annual kWh estimate |
| `TotalHt` / `TotalTva` / `TotalTtc` | decimal | Financial totals (copied from quote) |
| `CommercialUserId` | Guid? | Assigned commercial |
| `TechnicianUserId` | Guid? | Assigned technician |
| `ManagerUserId` | Guid? | Assigned manager |
| `ExpectedInstallationDate` | DateOnly? | Target install date |
| `LastActivityAt` | DateTimeOffset? | Last action timestamp |

---

## Project Detail API

`GET /api/v1/projects/{id}/detail`

Returns a complete aggregate including:
- Project info + solar sizing
- Client details
- Source lead & quote references
- Assigned users (commercial, technician, manager)
- All tasks with assignment and due dates
- Complete timeline
- Documents
- Contracts
- Invoices
- Financial summary

### Financial Summary

```json
{
  "quoteTotalTtc": 12000.000,
  "totalInvoiced": 12000.000,
  "totalPaid": 8000.000,
  "totalRemaining": 4000.000,
  "estimatedMargin": 2400.000,
  "marginPercent": 20.00,
  "fullyPaid": false
}
```

---

## Default Tasks on Project Creation

When a lead is marked Won, these tasks are automatically created:

| Task | Due Days |
|------|----------|
| Visite technique | +7 days |
| Validation toiture | +10 days |
| Dossier STEG | +14 days |
| Planification installation | +21 days |
| Validation finale client | +30 days |

---

## Timeline System

Every significant event is recorded as a `ProjectTimelineEvent`:

| Type | Trigger |
|------|---------|
| `ProjectCreated` | Mark-won automation |
| `StatusChanged` | Any status transition |
| `TaskCreated` | Manual task added |
| `TaskCompleted` | Task marked done |
| `InstallationPlanned` | Installation scheduled |
| `InstallationStarted` | Installation begins |
| `InstallationCompleted` | Installation done |
| `ContractGenerated` | Contract created |
| `InvoiceGenerated` | Invoice created |
| `PaymentReceived` | Payment recorded |
| `SAVCreated` | SAV ticket opened |
| `DocumentUploaded` | Document added |
| `CommentAdded` | Manual comment |
| `Note` | Free-text note |

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects` | List projects (paginated) — includes `reference`, `priority`, `totalTtc`, `commercialName` |
| GET | `/api/v1/projects/{id}` | Get project (summary) — full financial + commercial/manager/technician names |
| POST | `/api/v1/projects/{id}/assign-commercial` | Assign commercial user |
| GET | `/api/v1/projects/{id}/detail` | Get full project aggregate |
| POST | `/api/v1/projects` | Create project manually |
| PUT | `/api/v1/projects/{id}` | Update project |
| DELETE | `/api/v1/projects/{id}` | Soft-delete project |
| POST | `/api/v1/projects/{id}/change-status` | Workflow transition |
| GET | `/api/v1/projects/{id}/timeline` | Get timeline events |
| POST | `/api/v1/projects/{id}/timeline` | Add timeline event |
| GET | `/api/v1/projects/{id}/documents` | List documents |
| POST | `/api/v1/projects/{id}/documents` | Upload document |
| GET | `/api/v1/projects/{id}/invoices` | List invoices |
| GET | `/api/v1/projects/{id}/financial-summary` | Financial summary |
| GET | `/api/v1/projects/{id}/overview` | Task/installation summary |
| PATCH | `/api/v1/projects/{id}/progress` | Manual progress update |
| POST | `/api/v1/projects/{id}/assign-technician` | Assign technician |
| POST | `/api/v1/projects/{id}/assign-manager` | Assign manager |
