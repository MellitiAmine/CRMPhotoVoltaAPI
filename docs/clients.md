# Clients Module — API & Frontend Guide

> **Base URL:** `/api/v1/clients`  
> **Auth:** Bearer JWT (`TenantJwt`, claim `society_id` required)  
> **Angular route:** `/clients` (list) · `/clients/:id` (detail 360°)

---

## Overview

The **Clients** module is the central pivot **after the sale**. A client is a first-class entity linked to:

```
Client
 ├── Projects
 ├── Invoices
 ├── Installations (via projects)
 └── Payments (via invoices)
```

Use **`GET /clients/{id}/360`** for the detail page (single call, full history).

---

## Business rules

### Client activity (`active` / `inactive`)

| Status | Rule |
|--------|------|
| **active** | At least one project **not** `Completed` / `Cancelled`, **or** at least one non-cancelled invoice with **remaining balance** (`paidAmount < totalTtc`) |
| **inactive** | All projects terminal **and** no open invoice balance |

### Delete

`DELETE /clients/{id}` is allowed only when the client has **no projects** (soft-delete).

---

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/clients` | Paginated list + search + activity filter |
| GET | `/clients/{id}` | Basic client info (edit forms) |
| GET | `/clients/{id}/360` | **Vue 360°** — projects, invoices, installations, payments |
| POST | `/clients` | Create |
| PUT | `/clients/{id}` | Update |
| DELETE | `/clients/{id}` | Soft-delete |

---

## 1. List page — `GET /api/v1/clients`

### Query parameters

| Param | Type | Description |
|-------|------|-------------|
| `search` | string | Name, email, or phone (partial match) |
| `activity` | string | `active` \| `inactive` \| omit = all |
| `page` | int | Default `1` |
| `pageSize` | int | Default `20`, max `100` |
| `sortBy` | string | `name`, `email`, `phone`, or default `createdAt` |
| `sortOrder` | string | `asc` \| `desc` (default) |

### Example

```http
GET /api/v1/clients?search=dupont&activity=active&page=1&pageSize=20
```

### Response 200

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "name": "Famille Dupont",
      "email": "dupont@email.fr",
      "phone": "0612345678",
      "address": "12 Rue Victor Hugo, Tunis",
      "userId": null,
      "isActive": true,
      "projectCount": 2,
      "activeProjectCount": 1,
      "totalInvoicedTtc": 45000.000,
      "totalPaid": 20000.000,
      "totalRemaining": 25000.000,
      "lastActivityAt": "2026-07-01T14:30:00Z",
      "createdAt": "2025-11-10T09:00:00Z"
    }
  ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 48,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

### Angular list UI

| Column | Field |
|--------|-------|
| Nom | `name` |
| Téléphone | `phone` |
| Email | `email` |
| Projets | `projectCount` (`activeProjectCount` en badge) |
| Solde | `totalRemaining` |
| Statut | `isActive` → badge Actif / Inactif |
| Dernière activité | `lastActivityAt` |

Row click → navigate to `/clients/{id}`.

---

## 2. Detail page — `GET /api/v1/clients/{id}/360`

### Response structure

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "name": "Famille Dupont",
    "phone": "0612345678",
    "email": "dupont@email.fr",
    "address": "12 Rue Victor Hugo, Tunis",
    "userId": null,
    "createdAt": "2025-11-10T09:00:00Z",
    "updatedAt": "2026-06-15T10:00:00Z",
    "summary": {
      "isActive": true,
      "projectCount": 2,
      "activeProjectCount": 1,
      "installationCount": 3,
      "invoiceCount": 2,
      "paymentCount": 4,
      "totalInvoicedTtc": 45000.000,
      "totalPaid": 20000.000,
      "totalRemaining": 25000.000,
      "lastActivityAt": "2026-07-01T14:30:00Z"
    },
    "projects": [
      {
        "id": "uuid",
        "name": "Installation 6 kWc — Villa Dupont",
        "reference": "PRJ-2026-0042",
        "status": "Installation",
        "totalTtc": 28000.000,
        "progressPercent": 65,
        "commercialName": "Amir Moreau",
        "technicianName": "Lucas Bernard",
        "lastActivityAt": "2026-07-01T14:30:00Z",
        "createdAt": "2026-01-15T08:00:00Z"
      }
    ],
    "invoices": [
      {
        "id": "uuid",
        "projectId": "uuid",
        "projectName": "Installation 6 kWc — Villa Dupont",
        "reference": "FAC-2026-0012",
        "status": "PartiallyPaid",
        "invoiceDate": "2026-03-01",
        "totalTtc": 28000.000,
        "paidAmount": 15000.000,
        "remainingAmount": 13000.000
      }
    ],
    "installations": [
      {
        "id": "uuid",
        "projectId": "uuid",
        "projectName": "Installation 6 kWc — Villa Dupont",
        "technicianId": "uuid",
        "technicianName": "Lucas Bernard",
        "date": "2026-06-20",
        "status": "Completed",
        "createdAt": "2026-06-01T09:00:00Z"
      }
    ],
    "payments": [
      {
        "id": "uuid",
        "invoiceId": "uuid",
        "invoiceReference": "FAC-2026-0012",
        "projectId": "uuid",
        "projectName": "Installation 6 kWc — Villa Dupont",
        "amount": 10000.000,
        "paidOn": "2026-04-15",
        "method": "BankTransfer",
        "reference": "VIR-12345",
        "createdAt": "2026-04-15T11:00:00Z"
      }
    ]
  }
}
```

### Angular detail sections

| Section UI | Data source |
|------------|-------------|
| Infos client | Root fields (`name`, `phone`, `email`, `address`) |
| KPI strip | `summary` |
| Projets | `projects[]` → link `/projects/{id}` |
| Factures | `invoices[]` → link `/invoices/{id}` |
| Installations | `installations[]` → link `/installations/{id}` |
| Paiements | `payments[]` (historique global) |

### Angular example

```typescript
// clients-detail.component.ts
this.http.get<ApiResponse<Client360Dto>>(`/api/v1/clients/${id}/360`)
  .subscribe(res => this.client = res.data);
```

---

## 3. CRUD

### Create — `POST /api/v1/clients`

```json
{
  "name": "Famille Dupont",
  "phone": "0612345678",
  "email": "dupont@email.fr",
  "address": "12 Rue Victor Hugo, Tunis"
}
```

### Update — `PUT /api/v1/clients/{id}`

Same body shape as create.

### Basic get (edit modal) — `GET /api/v1/clients/{id}`

Returns `ClientDto` without related entities.

---

## Domain model

### `Client` (`app.Clients`)

| Column | Description |
|--------|-------------|
| `Id` | PK |
| `SocietyId` | Tenant |
| `Name` | Required |
| `Phone`, `Email`, `Address` | Contact |
| `UserId` | Optional portal user link |

### Relations

| Entity | FK |
|--------|-----|
| `Project` | `Project.ClientId` |
| `Invoice` | `Invoice.ClientId` |
| `Installation` | `Installation.ProjectId` → `Project.ClientId` |
| `Payment` | `Payment.InvoiceId` → `Invoice.ClientId` |

---

## Error codes

| Code | HTTP | When |
|------|------|------|
| `CLIENT_NOT_FOUND` | 404 | Unknown id |
| `CLIENT_HAS_PROJECTS` | 409 | Delete blocked |
| `VALIDATION_ERROR` | 400 | Missing name |
| `USER_NOT_IN_SOCIETY` | 400 | Invalid `userId` |

---

## Suggested Angular structure

```
src/app/features/clients/
├── clients.routes.ts          # /clients, /clients/:id
├── clients-list/
│   ├── clients-list.component.ts
│   └── clients-list.component.html
├── client-detail/
│   ├── client-detail.component.ts
│   └── sections/
│       ├── client-info-card.component.ts
│       ├── client-projects.component.ts
│       ├── client-invoices.component.ts
│       ├── client-installations.component.ts
│       └── client-payments.component.ts
└── models/client.models.ts      # mirror Client360Dto types
```

---

## Related APIs

| Need | Endpoint |
|------|----------|
| Project detail | `GET /api/v1/projects/{id}` |
| Record payment | `POST /api/v1/invoices/{id}/payments` |
| Installation detail | `GET /api/v1/installations/{id}` |
| Financial summary per project | `GET /api/v1/projects/{id}/financial-summary` |
