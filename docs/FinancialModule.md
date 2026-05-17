# Financial Module

## Overview

The financial module handles the complete revenue cycle for photovoltaic projects:

**Quote → Contract → Invoice → Payment → Paid**

All financial entities are tenant-scoped (`SocietyId`) and use `decimal` with `precision(18,3)` for currency accuracy. Currency default: **TND** (Tunisian Dinar).

---

## Contract Module

### Entity

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | PK |
| `SocietyId` | Guid | Tenant |
| `ProjectId` | Guid | Parent project |
| `ClientId` | Guid | Client |
| `Reference` | string(64) | Contract number |
| `Type` | ContractType | Installation / Maintenance / Warranty / Financing / Other |
| `Status` | ContractStatus | Draft / SentToClient / Signed / Cancelled |
| `SignedAt` | DateTimeOffset? | Signature date |
| `StartDate` / `EndDate` | DateOnly? | Validity range |
| `TotalAmount` | decimal(18,3) | Contract value |
| `PdfUrl` | string? | PDF link |

### Contract Lifecycle

```
Draft → SentToClient → Signed
      ↘ Cancelled
```

### Automations

When a contract is created:
- A `ContractGenerated` timeline event is added to the project
- A notification is sent to the assigned commercial

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects/{projectId}/contracts` | List contracts for project |
| GET | `/api/v1/contracts/{id}` | Get contract |
| POST | `/api/v1/contracts` | Create contract |
| PUT | `/api/v1/contracts/{id}` | Update status / PDF / dates |

### Create Contract Request

`clientId` is optional — if omitted, it is taken from the project.

```json
{
  "projectId": "...",
  "reference": "CTR-2026-0001",
  "type": "Installation",
  "startDate": "2026-06-01",
  "endDate": "2026-08-31",
  "totalAmount": 12000.000,
  "notes": "Contrat installation PV 6 kWc",
  "pdfUrl": "https://storage/contracts/CTR-2026-0001.pdf"
}
```

---

## Invoice Module

### Entity

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | PK |
| `SocietyId` | Guid | Tenant |
| `ProjectId` | Guid | Parent project |
| `ClientId` | Guid | Client |
| `Reference` | string(64) | **Unique per society** |
| `Status` | InvoiceStatus | Draft / Sent / PartiallyPaid / Paid / Overdue / Cancelled |
| `InvoiceDate` | DateOnly | Issue date |
| `DueDate` | DateOnly? | Payment due |
| `TotalHt` | decimal(18,3) | Before tax total |
| `TotalTva` | decimal(18,3) | Tax total |
| `TotalTtc` | decimal(18,3) | Grand total |
| `PaidAmount` | decimal(18,3) | Sum of payments received |
| `RemainingAmount` | computed | `TotalTtc - PaidAmount` |

### Invoice Lifecycle

```
Draft → Sent → PartiallyPaid → Paid
     ↘ Overdue
     ↘ Cancelled
```

### Tax Calculation

For each line:
```
TotalHt = Quantity × UnitPrice
LineTva = TotalHt × TvaRate / 100
```

Invoice totals:
```
TotalHt  = Σ lines TotalHt
TotalTva = Σ (TotalHt × TvaRate / 100)
TotalTtc = TotalHt + TotalTva
```

Default TVA rate: **19%**

### Invoice Lines (InvoiceItem)

| Field | Type |
|-------|------|
| `ItemId` | Guid? (catalog ref) |
| `Description` | string(500) |
| `Quantity` | decimal(18,2) |
| `UnitPrice` | decimal(18,3) |
| `TvaRate` | decimal(5,2) |
| `TotalHt` | decimal(18,3) |

### Payment Recording

`POST /api/v1/invoices/{id}/payments`

```json
{
  "amount": 4000.000,
  "paidOn": "2026-06-15",
  "method": "BankTransfer",
  "reference": "VIR-2026-0042",
  "notes": "Acompte 1/3"
}
```

**Payment methods:** `BankTransfer`, `Cash`, `Cheque`, `CreditCard`, `Other`

**Automations on payment:**
- Updates `PaidAmount` on invoice
- Sets status to `PartiallyPaid` or `Paid` automatically
- Adds `PaymentReceived` timeline event
- If fully paid: notifies commercial

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects/{id}/invoices` | List invoices for project |
| GET | `/api/v1/invoices/{id}` | Get invoice with lines & payments |
| POST | `/api/v1/invoices` | Create invoice |
| PUT | `/api/v1/invoices/{id}` | Update status / due date |
| POST | `/api/v1/invoices/{id}/payments` | Record a payment |
| GET | `/api/v1/projects/{id}/financial-summary` | Project financial summary |

---

## Financial Summary

`GET /api/v1/projects/{id}/financial-summary`

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

| Field | Description |
|-------|-------------|
| `quoteTotalTtc` | Accepted quote value |
| `totalInvoiced` | Sum of non-cancelled invoice TTC |
| `totalPaid` | Sum of all payments |
| `totalRemaining` | `totalInvoiced - totalPaid` |
| `estimatedMargin` | From `Project.EstimatedMargin` |
| `marginPercent` | `estimatedMargin / quoteTotalTtc × 100` |
| `fullyPaid` | `true` when remaining ≤ 0 |

---

## Document Module

### ProjectDocument Entity

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | PK |
| `ProjectId` | Guid | Parent project |
| `Type` | ProjectDocumentType | Category |
| `Name` | string(300) | Display name |
| `Url` | string(1000) | Storage URL |
| `UploadedByUserId` | Guid? | User who uploaded |
| `UploadedAt` | DateTimeOffset | Upload timestamp |

### Document Types

`Quote`, `Contract`, `Invoice`, `TechnicalStudy`, `STEG`, `InstallationPhoto`, `ClientDocument`, `SAV`, `Other`

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects/{id}/documents` | List project documents |
| POST | `/api/v1/projects/{id}/documents` | Add document |

```json
{
  "type": "TechnicalStudy",
  "name": "Étude dimensionnement PV 6kWc",
  "url": "https://storage/docs/etude-pv-2026.pdf"
}
```

Adding a document auto-creates a `DocumentUploaded` timeline event.
