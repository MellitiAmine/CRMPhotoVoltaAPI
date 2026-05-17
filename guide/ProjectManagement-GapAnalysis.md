# Photovoltaic CRM — project management gap analysis

Stack in this repo: **ASP.NET Core**, **EF Core**, **PostgreSQL**, **Guid** keys, **`SocietyId`** multi-tenancy (not SQL Server / `int` as in generic specs).

---

## What you already have (reusable)

| Area | Status | Notes |
|------|--------|--------|
| **Multi-tenant `SocietyId`** | Done | `SocietyScopedEntity`, global query filters on `AppDbContext`, JWT `society_id` |
| **Leads** | Done | CRUD, pipeline statuses, assign, activities, scoring LVI/SD, automation (WhatsApp rec.) |
| **`POST /leads/{id}/mark-won`** | Partial | Sets status **Gagné** + activity only — **no client/project automation** |
| **`POST /leads/{id}/convert`** | Done | Creates **Client** (+ optional Deal), status **Converti** — separate from **Gagné** |
| **Clients** | Done | Basic CRM client |
| **Deals / pipeline stages** | Done | Deal linked to lead |
| **Quotes + items** | Done | HT/TVA/TTC, catalog items, `convert-to-project` from **accepted** quote only |
| **Project (basic)** | Done | Client, optional Deal, `SystemSizeKw`, manager/technician, progress %, 4 statuses |
| **Project stages** | Done | `ProjectStage` + `ProjectStageTracking` (configurable checklist per stage) |
| **Tasks** | Partial | `CrmTask` on project (title, assignee, due, Open/Done) — no Kanban priority/description |
| **Installations** | Partial | Schedule, start/complete, checklist, photos — no GPS/signature/report |
| **Dashboard** | Partial | KPIs, pipeline, revenue (quotes), project counts — not PV-specific PM dashboard |
| **Calendar** | Done | Events API |
| **Notifications** | Done | In-app `INotificationService` |
| **Roles** | Done | Admin / Manager / Commercial / Technician via `RoleType` + policies |
| **Documents** | Done | Generic file metadata |
| **Reports** | Done | Basic reporting service |

---

## Gap vs target “full PV CRM / ERP”

### Domain & data model

| Target | Gap |
|--------|-----|
| **Project** with LeadId, QuoteId, reference, PV fields, financials | Missing **LeadId**, **QuoteId**, **Reference**, **Priority**, **CommercialUserId**, **Notes**, **ExpectedInstallationDate**, **LastActivityAt**, roof/panel/inverter fields, **TotalHt/Tva/Ttc** on project |
| **15+ project statuses** (Study → STEG → SAV) | Only **Planned / InProgress / Done / Cancelled** |
| **ProjectTask** (rich tasks) | **CrmTask** is minimal (no description, priority, completed timestamp) |
| **ProjectTimelineEvent** | **Missing** (lead activities ≠ project timeline) |
| **SAV / after-sales** | **Missing** entity & APIs |
| **Stock / inventory** | **Missing** (no reservation/deduction) |
| **Equipment catalog → project BOM** | Items catalog exists for **quotes**, not project stock |

### Workflow & automation

| Target | Gap |
|--------|-----|
| **mark-won → client + project + quote link + tasks + timeline + notify** (1 transaction) | **Not implemented** |
| Idempotent mark-won (no duplicate projects) | **Missing** |
| Status change → timeline events | **Missing** |
| Installation status → stock reserve/deduct | **Missing** |
| Quote **Accepted** auto-sync to project financials | Only manual **convert-to-project** |

### APIs

| Target | Gap |
|--------|-----|
| Project PM CRUD (extended fields) | Partial |
| Project timeline API | **Missing** |
| Project tasks API (dedicated) | Only via generic tasks / none |
| Project dashboard (by status, delays, forecast) | Partial (`DashboardProjectsDto` only totals) |
| Installation calendar / workload | Partial (installations exist, no aggregated API) |
| SAV CRUD | **Missing** |

### Frontend (Angular)

| Screen | Gap |
|--------|-----|
| Project list / detail | Likely partial if any — not in this API repo |
| Timeline component | **Missing** |
| Tasks Kanban | **Missing** |
| Installation planner calendar | **Missing** |
| SAV module | **Missing** |
| mark-won UX showing created project | **Missing** |

### Security

| Item | Status |
|------|--------|
| SocietyId on all rows | Done for existing entities |
| Cross-tenant checks in services | Done pattern (`societyId` + query filters) |
| Fine-grained project permissions | Partial (society roles exist; no project-level ACL) |

---

## Recommended target architecture (modules)

```
Lead (CRM) ──mark-won──► LeadWonOrchestrator (transaction)
                              ├── Client (find or create)
                              ├── Project (PV lifecycle)
                              ├── Quote link + financial copy
                              ├── ProjectTimelineEvents
                              ├── ProjectTasks (extend CrmTask or new table)
                              ├── Default installation prep
                              └── Notifications

Project ──► Timeline (audit)
         ──► Tasks (Kanban)
         ──► Installations (field)
         ──► SAV tickets (after-sales)
         ──► Stock movements (future)

Dashboard ──► PM aggregates (status funnel, delays, revenue from project totals)
```

---

## Implementation phases (suggested)

1. **Phase A (this sprint)** — `mark-won` orchestration + extend `Project` + `ProjectTimelineEvent` + default tasks + notification (transactional).
2. **Phase B** — Full `ProjectStatus` workflow + timeline on status change + extended project APIs/DTOs.
3. **Phase C** — Installation enhancements (GPS, signature, report) + installation calendar API.
4. **Phase D** — SAV module.
5. **Phase E** — Stock / BOM integration.
6. **Phase F** — Angular PM suite (list, detail, timeline, Kanban, calendars).

See also: [ItemsAndQuotes.md](ItemsAndQuotes.md), [TenantSide.md](TenantSide.md).
