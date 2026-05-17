# API Reference

Base URL: `https://{host}/api/v1`

All endpoints require `Authorization: Bearer {tenantJwt}` unless marked otherwise.

---

## Authentication

### Tenant JWT Claims

| Claim | Description |
|-------|-------------|
| `sub` | User ID (Guid) |
| `society_id` | Tenant society ID (Guid) |
| `role` | User role: Admin, Manager, Commercial, Technician |
| `email` | User email |

---

## Leads

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/leads` | — | Paged `LeadListItemDto[]` |
| GET | `/leads/{id}` | — | `LeadDto` |
| POST | `/leads` | `CreateLeadRequest` | `LeadDto` |
| PUT | `/leads/{id}` | `UpdateLeadRequest` | `LeadDto` |
| DELETE | `/leads/{id}` | — | `{deleted: true}` |
| GET | `/leads/{id}/activities` | — | `LeadActivityDto[]` |
| POST | `/leads/{id}/activities` | `AddLeadActivityRequest` | `LeadActivityDto` |
| POST | `/leads/{id}/assign` | `AssignLeadRequest` | `LeadDto` |
| POST | `/leads/{id}/convert` | `ConvertLeadRequest` | `ConvertLeadResultDto` |
| POST | `/leads/{id}/mark-won` | — | `LeadWonResultDto` |
| POST | `/leads/{id}/mark-lost` | — | `LeadDto` |
| POST | `/leads/{id}/notes` | `AddLeadNoteRequest` | `LeadActivityDto` |
| GET | `/leads/{id}/timeline` | — | `LeadTimelineEntryDto[]` |
| POST | `/leads/{id}/change-status` | `ChangeLeadStatusRequest` | `LeadDto` |
| POST | `/leads/{id}/change-temperature` | `ChangeLeadTemperatureRequest` | `LeadDto` |
| POST | `/leads/{id}/tags` | `AddLeadTagRequest` | `LeadDto` |
| DELETE | `/leads/{id}/tags/{tag}` | — | `LeadDto` |
| POST | `/leads/{id}/recalculate-score` | — | `LeadDto` |

---

## Projects

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/projects` | — | Paged `ProjectListItemDto[]` |
| GET | `/projects/{id}` | — | `ProjectDto` |
| GET | `/projects/{id}/detail` | — | `ProjectDetailDto` |
| POST | `/projects` | `CreateProjectRequest` | `ProjectDto` |
| PUT | `/projects/{id}` | `UpdateProjectRequest` | `ProjectDto` |
| DELETE | `/projects/{id}` | — | `{deleted: true}` |
| POST | `/projects/{id}/change-status` | `ChangeProjectStatusRequest` | `ProjectDto` |
| GET | `/projects/{id}/timeline` | — | `ProjectTimelineEventDto[]` |
| POST | `/projects/{id}/timeline` | `AddTimelineEventRequest` | `ProjectTimelineEventDto` |
| GET | `/projects/{id}/documents` | — | `ProjectDocumentDto[]` |
| POST | `/projects/{id}/documents` | `UploadProjectDocumentRequest` | `ProjectDocumentDto` |
| GET | `/projects/{id}/invoices` | — | `InvoiceSummaryDto[]` |
| GET | `/projects/{id}/financial-summary` | — | `FinancialSummaryDto` |
| GET | `/projects/{id}/overview` | — | `ProjectOverviewDto` |
| GET | `/projects/{id}/progress` | — | `ProjectProgressDto` |
| PATCH | `/projects/{id}/progress` | `PatchProjectProgressRequest` | `ProjectDto` |
| POST | `/projects/{id}/assign-technician` | `AssignProjectUserRequest` | `ProjectDto` |
| POST | `/projects/{id}/assign-manager` | `AssignProjectUserRequest` | `ProjectDto` |
| GET | `/projects/{projectId}/contracts` | — | `ContractDto[]` |

---

## Contracts

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/projects/{projectId}/contracts` | — | `ContractDto[]` |
| GET | `/contracts/{id}` | — | `ContractDto` |
| POST | `/contracts` | `CreateContractRequest` | `ContractDto` |
| PUT | `/contracts/{id}` | `UpdateContractRequest` | `ContractDto` |

---

## Invoices

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/projects/{id}/invoices` | — | `InvoiceDto[]` |
| GET | `/invoices/{id}` | — | `InvoiceDto` |
| POST | `/invoices` | `CreateInvoiceRequest` | `InvoiceDto` |
| PUT | `/invoices/{id}` | `UpdateInvoiceRequest` | `InvoiceDto` |
| POST | `/invoices/{id}/payments` | `AddPaymentRequest` | `InvoiceDto` |

---

## Quotes

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/quotes` | — | Paged `QuoteDto[]` |
| GET | `/quotes/{id}` | — | `QuoteDto` |
| POST | `/quotes` | `CreateQuoteRequest` | `QuoteDto` |
| PUT | `/quotes/{id}` | `UpdateQuoteRequest` | `QuoteDto` |
| POST | `/quotes/{id}/send` | — | `QuoteDto` |
| POST | `/quotes/{id}/accept` | — | `QuoteDto` |
| POST | `/quotes/{id}/reject` | — | `QuoteDto` |
| POST | `/quotes/{id}/convert-to-project` | `ConvertQuoteToProjectRequest` | `QuoteDto` |
| GET | `/quotes/{id}/items` | — | `QuoteItemDto[]` |
| POST | `/quote-items` | `CreateQuoteItemRequest` | `QuoteItemDto` |
| PUT | `/quote-items/{id}` | `UpdateQuoteItemRequest` | `QuoteItemDto` |
| DELETE | `/quote-items/{id}` | — | `{deleted: true}` |

---

## Items (Catalog)

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/items` | — | `ItemDto[]` |
| POST | `/items` | `CreateItemRequest` | `ItemDto` |
| DELETE | `/items/{id}` | — | `{deleted: true}` |

---

## Dashboard

| Method | Endpoint | Response |
|--------|----------|----------|
| GET | `/dashboard` | `DashboardDto` |

---

## Clients

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| GET | `/clients` | — | Paged `ClientDto[]` |
| GET | `/clients/{id}` | — | `ClientDto` |
| POST | `/clients` | `CreateClientRequest` | `ClientDto` |
| PUT | `/clients/{id}` | `UpdateClientRequest` | `ClientDto` |

---

## Standard Response Envelope

```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

### Error Response

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "Project not found.",
    "statusCode": 404
  }
}
```

---

## Pagination

Query params: `?page=1&pageSize=20&search=&sortBy=createdAt&sortOrder=desc`

Response includes:
```json
{
  "items": [...],
  "meta": {
    "total": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

---

## DTO Examples

### ProjectDetailDto (abbreviated)

```json
{
  "id": "3fa85f64-...",
  "name": "Projet PV — Ahmed Ben Ali",
  "reference": "PRJ-2026-0012",
  "status": "Installation",
  "progressPercent": 75,
  "systemSizeKw": 6.0,
  "client": {
    "id": "...",
    "name": "Ahmed Ben Ali",
    "phone": "+216 55 000 111",
    "email": "ahmed@example.com"
  },
  "commercial": {
    "id": "...",
    "fullName": "Sonia Gharbi",
    "email": "sonia@solarco.tn"
  },
  "financial": {
    "quoteTotalTtc": 12000.000,
    "totalInvoiced": 12000.000,
    "totalPaid": 8000.000,
    "totalRemaining": 4000.000,
    "marginPercent": 20.00,
    "fullyPaid": false
  },
  "tasks": [
    {
      "id": "...",
      "title": "Visite technique",
      "status": "Done",
      "dueDate": "2026-06-05",
      "completedAt": "2026-06-04T10:30:00Z"
    }
  ],
  "timeline": [
    {
      "type": "StatusChanged",
      "description": "Statut changé: Approved → Planning",
      "createdByName": "Sonia Gharbi",
      "createdAt": "2026-06-10T08:00:00Z"
    }
  ]
}
```

### InvoiceDto (abbreviated)

```json
{
  "id": "...",
  "reference": "FAC-2026-0001",
  "status": "PartiallyPaid",
  "invoiceDate": "2026-06-15",
  "dueDate": "2026-07-15",
  "totalHt": 10084.034,
  "totalTva": 1915.966,
  "totalTtc": 12000.000,
  "paidAmount": 8000.000,
  "remainingAmount": 4000.000,
  "items": [
    {
      "description": "Panneaux solaires 400W × 15",
      "quantity": 15,
      "unitPrice": 350.000,
      "tvaRate": 19,
      "totalHt": 5250.000
    }
  ],
  "payments": [
    {
      "amount": 8000.000,
      "paidOn": "2026-06-20",
      "method": "BankTransfer",
      "reference": "VIR-2026-0001"
    }
  ]
}
```
