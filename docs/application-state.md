# CRM PhotoVolta — Application State & Progress

> **Last updated:** July 2026  
> **Stack:** ASP.NET Core 8 · EF Core · PostgreSQL · Multi-tenant (`SocietyId` + JWT)

---

## What this application is

A **multi-tenant CRM/ERP API** for photovoltaic installers in Tunisia. Each **society** (tenant) manages the full commercial cycle:

**Lead → Client → Quote → Project → Contract → Invoice → Payment → Installation**

There is also a **platform** layer (super-admin) for societies, subscriptions, and provisioning.

---

## Overall progress

| Layer | Status | Notes |
|-------|--------|-------|
| **API backend** | ~75% | Core CRM + finance + field ops are usable |
| **Documentation** | Good | Module docs under `docs/` |
| **Frontend** | External | This repo is API-only; Angular app consumes these endpoints |
| **Production readiness** | Partial | Auth, tenancy, and file storage need prod hardening |

---

## Module status

### Done (production-usable with frontend)

| Module | Base route | Highlights |
|--------|------------|------------|
| **Auth & tenancy** | `/api/v1/auth` | JWT, `society_id`, role policies |
| **Users & roles** | `/users`, `/roles` | Admin, Manager, Commercial, Technicien |
| **Leads** | `/leads` | CRUD, assign, activities, journal, scoring (LVI/SD), tags, mark-won/lost |
| **Clients** | `/clients` | **360° view** — list enrichie, détail projets/factures/installations/paiements |
| **Deals** | `/deals` | CRM pipeline |
| **Quotes & items** | `/quotes`, `/items` | HT/TVA/TTC, catalog, convert to project |
| **Projects** | `/projects` | Extended PV fields, timeline, tasks, documents, installations |
| **Contracts** | `/contracts` | Lifecycle + timeline events |
| **Invoices & payments** | `/invoices` | Create, update, record payments, financial summary |
| **Installations** | `/installations` | Schedule, start/complete, checklist CRUD, photo upload |
| **Commercials** | `/commercials` | HR profiles, KPIs, score, calendar |
| **Techniciens** | `/techniciens` | Same pattern as commercials (field staff) |
| **Calendar** | `/calendar` | Events, participants, technician/commercial views |
| **Notifications** | `/notifications` | In-app |
| **Dashboard & reports** | `/dashboard`, `/reports` | KPIs, basic reports |
| **File uploads** | `/files/...` | Local `wwwroot` storage (configurable) |
| **Platform admin** | `/api/v1/platform/*` | Societies, subscriptions, plans |

### Partial (works but incomplete)

| Module | What's missing |
|--------|----------------|
| **Lead mark-won** | Orchestration exists; verify end-to-end with your frontend flows |
| **Project tasks** | Basic `CrmTask` only — no Kanban, priority, rich description |
| **Project statuses** | Extended enum exists but not full STEG/SAV workflow |
| **Dashboard** | Not yet a dedicated PV project-manager view (delays, workload) |
| **Documents** | Upload works; no Cloudinary/S3 provider yet (config placeholder only) |
| **Installations** | No GPS, signature capture, or PDF report |
| **Commercial / technicien KPIs** | Snapshot fields exist; no automatic nightly sync from CRM data |
| **SAV (after-sales)** | Not implemented |
| **Stock / inventory** | Not implemented |

### Not started

- SAV tickets and warranty tracking  
- Equipment BOM / stock reservation on installation  
- Email/SMS notifications (only in-app today)  
- Webhooks / SignalR real-time  
- Cloud file storage (Cloudinary)  
- Automated backup & observability (metrics, tracing)

---

## Recent progress (2026)

- **Techniciens API** — full CRUD mirroring commercials (`/techniciens`)
- **Installation checklist** — full CRUD + initialize defaults
- **File uploads** — photos & project documents via `multipart/form-data` → `wwwroot/files`
- **Invoice payments** — concurrency fix on `POST /invoices/{id}/payments`
- **Project extensions** — timeline events, financial module (contracts, invoices, documents)

---

## Documentation map

| Topic | File |
|-------|------|
| API overview | `docs/ApiReference.md` |
| Leads | `docs/leads-api.md` |
| Projects | `docs/ProjectManagement.md` |
| Finance | `docs/FinancialModule.md` |
| Commercials | `docs/commercial.md` |
| Techniciens | `docs/technicien.md` |
| Installations checklist | `docs/installation-checklist.md` |
| Installation planning | `docs/installation-planning.md` |
| File uploads | `docs/file-uploads.md` |
| Clients | `docs/clients.md` |
| Auth & tenancy | `docs/tenant-auth.md`, `docs/MultiTenancy.md` |
| Calendar | `docs/calendar.md` |
| Frontend guides | `guide/` folder |

---

## Suggestions (priority order)

### 1. Short term (stabilize what exists)

1. **Frontend alignment** — migrate all uploads to `FormData` (`docs/file-uploads.md`); stop sending raw URLs.
2. **Payment & invoice UX** — always send `paidOn` (ISO date) and positive `amount`; reload invoice on 409.
3. **Production config** — set strong JWT keys, `FileStorage:PublicBaseUrl`, and remove demo seeds.
4. **E2E smoke tests** — lead → quote → project → invoice → payment → installation checklist.

### 2. Medium term (business value)

1. **Automatic KPI sync** — nightly job to push leads/deals/installations into commercial & technicien scores.
2. **Lead mark-won polish** — single idempotent flow: client + project + quote link + timeline + notify.
3. **Installation planner API** — aggregated calendar by technician (workload view).
4. **Cloudinary provider** — implement `IFileStorageService` for production file hosting.
5. **Project task API** — dedicated endpoints with priority, description, Kanban status.

### 3. Long term (full ERP)

1. **SAV module** — tickets linked to projects/clients after installation.
2. **Stock & BOM** — reserve panels/inverters on installation start, deduct on complete.
3. **STEG / administrative workflow** — extra project statuses and checklist templates.
4. **Notifications** — email when invoice paid, installation completed, lead assigned.
5. **Observability** — structured logs, health checks per DB schema, error tracking (Sentry).

---

## Quick health check

```bash
# API running
GET /api/health

# Apply migrations
dotnet ef database update --context AppDbContext
dotnet ef database update --context CoreDbContext
dotnet ef database update --context PlatformDbContext
```

---

## Summary

The API covers the **main PV installer workflow** end-to-end: commercial pipeline, project delivery, billing, and field installations. The biggest gaps for a “complete ERP” are **SAV**, **stock**, **rich task management**, and **production infrastructure** (cloud files, monitoring). Focus next on **frontend integration** of recent APIs and **hardening** payment/upload flows before adding new modules.
