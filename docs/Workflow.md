# Workflow & Automation Documentation

## Lead → Won → Project: Complete Lifecycle

### 1. Lead Creation

A lead enters the CRM at status `Nouveau`. The scoring engine computes two scores:

- **LVI** (Lead Value Index) — potential/financial score
- **SD** (Sales Decision score) — urgency/readiness score

### 2. Lead Scoring

Automatic after every update. Components:
- **Interaction** (25%) — calls, visits, WhatsApp
- **Intention** (20%) — quote requests, buying signals
- **Satisfaction** (15%) — ratings, positive reactions
- **Activity** (20%) — recent CRM activities
- **Potential** (20%) — monthly bill, kW estimate, budget
- **Penalties** — stale lead, long since last contact

**Temperature** auto-sets: Hot (SD ≥ 85), Warm (SD ≥ 60), Neutral (SD ≥ 40), Cool (SD ≥ 20), Cold.

### 3. Lead Won Automation

`POST /api/v1/leads/{id}/mark-won`

Executes in a **single DB transaction**:

```
1. Find or create Client (match by email or phone)
2. Find latest Accepted Quote (or most recent quote)
3. Find associated Deal
4. Generate project reference: PRJ-{year}-####
5. Create Project (copy financials from quote)
6. Create 5 default CrmTasks
7. Add ProjectTimelineEvent: ProjectCreated
8. Update Lead status → Gagné
9. Record LeadActivity: StatusChange
10. If quote exists: set ProjectId, status → Converted
11. COMMIT
12. Recalculate lead score (async, outside transaction)
13. Notify assigned commercial
```

**Idempotency:** If a project already exists for this lead → `409 PROJECT_ALREADY_EXISTS`

**Response (`LeadWonResultDto`):**

```json
{
  "lead": { ... },
  "clientId": "uuid",
  "projectId": "uuid",
  "quoteId": "uuid",
  "clientCreated": true,
  "projectCreated": true
}
```

### 4. Project Status Workflow

`POST /api/v1/projects/{id}/change-status`

```json
{
  "status": "TechnicalVisit",
  "comment": "Visite planifiée le 20 juin"
}
```

**Response:** Updated `ProjectDto`

On each transition:
- Validates allowed path
- Updates `ProgressPercent` automatically
- Creates `StatusChanged` timeline event
- Notifies commercial

### 5. Contract Generation

`POST /api/v1/contracts`

On creation:
- Creates `ContractGenerated` timeline event
- Notifies commercial

### 6. Invoice Generation

`POST /api/v1/invoices`

On creation:
- Calculates line totals (HT, TVA, TTC)
- Creates `InvoiceGenerated` timeline event
- Notifies commercial

### 7. Payment Recording

`POST /api/v1/invoices/{id}/payments`

On payment:
- Updates `Invoice.PaidAmount`
- Auto-sets status: `PartiallyPaid` or `Paid`
- Creates `PaymentReceived` timeline event
- If fully paid: notifies commercial

### 8. Document Upload

`POST /api/v1/projects/{id}/documents`

On upload:
- Creates `DocumentUploaded` timeline event

---

## Automation Summary Table

| Trigger | Automation |
|---------|------------|
| Lead marked Won | Create client + project + 5 tasks + timeline + notify |
| Project status change | Create timeline event + update progress + notify |
| Contract created | Timeline event + notify commercial |
| Invoice created | Timeline event + notify commercial |
| Invoice fully paid | Timeline event + notify commercial + set status Paid |
| Document uploaded | Timeline event |
| Lead score updated | SD routing decision (WhatsApp / call suggestion) |

---

## Role-Based Visibility

| Role | Leads | Projects | Invoices | Contracts |
|------|-------|----------|----------|-----------|
| Admin | All | All | All | All |
| Manager | All | All | All | All |
| Commercial | Assigned only | Assigned only | Own projects | Own projects |
| Technician | — | Assigned only | — | — |

All queries are filtered by `SocietyId` first (multi-tenancy), then by user assignment for Commercial/Technician roles.

---

## Error Codes Reference

| Code | HTTP | Meaning |
|------|------|---------|
| `LEAD_NOT_FOUND` | 404 | Lead does not exist in this society |
| `PROJECT_NOT_FOUND` | 404 | Project does not exist in this society |
| `PROJECT_ALREADY_EXISTS` | 409 | Mark-won called twice on same lead |
| `INVALID_TRANSITION` | 400 | Invalid project status transition |
| `CONTRACT_NOT_FOUND` | 404 | Contract not found |
| `INVOICE_NOT_FOUND` | 404 | Invoice not found |
| `DUPLICATE_REFERENCE` | 409 | Invoice reference already used |
| `TENANT_REQUIRED` | 403 | Missing `society_id` JWT claim |
| `TENANT_MISMATCH` | 403 | Query param society_id ≠ JWT |
| `UNAUTHORIZED` | 401 | Missing user identity |
| `VALIDATION_ERROR` | 400 | Input validation failed |
