# Items & quotes — API guide

Technical reference for the **catalog items** (`/api/v1/items`) and **quotes** (`/api/v1/quotes`, `/api/v1/quote-items`). For tenant login and JWT basics, see [TenantSide.md](TenantSide.md).

All routes require a **tenant JWT**:

`Authorization: Bearer <tenant_access_token>`

Every row is scoped by **`society_id`** in the token. Data is stored with **`SocietyId`** (UUID) in the database.

---

## Optional query `societyId`

On **items**, **quote-items**, and **quotes** (list + create only), you may pass:

`?societyId=<uuid>`

Rules:

- If **omitted**, the society comes **only** from the JWT (recommended).
- If **present**, it **must equal** the JWT `society_id` or the API returns **`403`** with code **`TENANT_MISMATCH`** (no cross-tenant access).

---

## Response envelope

Successful JSON matches the shared shape:

```json
{
  "success": true,
  "data": { },
  "meta": null,
  "error": null
}
```

Paged list (`GET /api/v1/quotes`):

```json
{
  "success": true,
  "data": [ ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 0,
    "totalPages": 0,
    "hasNext": false,
    "hasPrevious": false
  },
  "error": null
}
```

Errors use `success: false` and `error: { "code", "message", "details" }`.

---

## 1) Catalog items (`/api/v1/items`)

Multi-tenant catalog lines (name, unit, default price, default VAT rate).

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/v1/items` | List items for the current society (ordered by name). |
| `POST` | `/api/v1/items` | Create an item. |
| `DELETE` | `/api/v1/items/{id}` | Soft-delete an item (`IsDeleted`). Optional `societyId` query (must match JWT). |

### `GET /api/v1/items`

Query: optional `societyId` (see above).

**Response `data`:** array of `ItemDto`

| Field | Type | Notes |
|-------|------|--------|
| `id` | UUID | Primary key |
| `societyId` | UUID | Tenant |
| `name` | string | Required on create |
| `reference` | string? | Optional SKU / internal ref |
| `unit` | string | e.g. `piece`, `meter` (default `piece`) |
| `defaultPrice` | decimal | Precision 18,3 |
| `tvaRate` | decimal | Percent, precision 5,2 (e.g. `7`, `19`) |
| `createdAt` | date-time | UTC offset |

### `POST /api/v1/items`

Body: `CreateItemRequest`

| Field | Type | Required | Notes |
|-------|------|----------|--------|
| `name` | string | yes | Trimmed |
| `reference` | string? | no | |
| `unit` | string | no | Default `piece` |
| `defaultPrice` | decimal | yes | |
| `tvaRate` | decimal | yes | 0–100 |

**Response:** `201` with `data` = created `ItemDto`.

---

## 2) Quotes (`/api/v1/quotes`)

Quotes support **header totals** (HT, TVA, TTC) and **lines** with optional link to a catalog **item**.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/v1/quotes` | Paged list. |
| `GET` | `/api/v1/quotes/{id}` | Full quote + lines. |
| `POST` | `/api/v1/quotes` | Create draft quote + optional embedded lines. |
| `PUT` | `/api/v1/quotes/{id}` | Replace draft quote (full payload including lines). |
| `DELETE` | `/api/v1/quotes/{id}` | Soft-delete (blocked if accepted/converted). |
| `POST` | `/api/v1/quotes/{id}/send` | Mark sent. |
| `POST` | `/api/v1/quotes/{id}/accept` | Accept. |
| `POST` | `/api/v1/quotes/{id}/reject` | Reject. |
| `POST` | `/api/v1/quotes/{id}/convert-to-project` | Create project from accepted quote. |

List and create accept optional `societyId` (same validation as items). **`Get` / `Put` / `Delete` / lifecycle** use the JWT society only (no `societyId` query on those handlers).

### `GET /api/v1/quotes` — pagination

| Query | Default | Notes |
|-------|---------|--------|
| `page` | `1` | |
| `pageSize` | `20` | Clamped between **10** and **50** server-side |
| `sortOrder` | `desc` | `asc` / `desc` (creation time) |
| `search` | — | Matches title / quote number (case-insensitive) |
| `societyId` | — | Optional; must match JWT if set |

**List row (`QuoteListItemDto`):** includes `totalAmount`, `totalHt`, `totalTva`, `totalTtc`, `currency`, `status`, `leadId`, `clientId`, etc.

### `POST` / `PUT` — body shapes

**`CreateQuoteRequest`**

| Field | Notes |
|-------|--------|
| `title` | Required |
| `currency` | Default `TND` |
| `leadId`, `clientId`, `dealId` | Optional UUIDs; must belong to same society |
| `validUntil` | Optional `date` |
| `items` | Array of `QuoteItemWriteDto` (can be empty) |

**`QuoteItemWriteDto`** (embedded lines on create/update)

| Field | Notes |
|-------|--------|
| `itemId` | Optional. If set, must exist in **`Items`** for this society. |
| `description` | Required **unless** `itemId` is set (then defaults to item name). |
| `quantity` | Default `1`; values ≤ 0 are treated as `1` when replacing lines from quote create/update |
| `unitPrice` | If `itemId` set and `unitPrice` ≤ 0, server uses **`defaultPrice`** from catalog |
| `discount` | Optional; default `0`. Percent **0–100** |
| `tvaRate` | Optional. If `itemId` set and omitted, uses catalog **`tvaRate`**; else `0` |
| `sortOrder` | Line order |

**Totals (server-calculated)**

For each line:

\[
\text{lineTotalHt} = \text{quantity} \times \text{unitPrice} \times \left(1 - \frac{\text{discount}}{100}\right)
\]

For the quote:

- `totalHt` = sum of lines’ `totalHt`
- `totalTva` = sum of (`line.totalHt × line.tvaRate / 100`)
- `totalTtc` = `totalHt + totalTva`
- **`totalAmount`** is kept equal to **`totalTtc`** for backward compatibility

`quoteDate` is set when the quote is **created** (document date).

Only **`Draft`** quotes can be edited via **`PUT`** (full replace of lines). Use **`/api/v1/quote-items`** for incremental line edits on drafts.

---

## 3) Quote lines (`/api/v1/quote-items`)

Add, update, or remove **one** line on a **draft** quote; the server recomputes quote HT / TVA / TTC and returns the **full `QuoteDto`**.

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/v1/quote-items` | Add a line. |
| `PUT` | `/api/v1/quote-items/{id}` | Update line `{id}`. |
| `DELETE` | `/api/v1/quote-items/{id}` | Remove line `{id}`. |

Optional query: `societyId` (same rules as above).

### `POST /api/v1/quote-items` — `CreateQuoteItemLineRequest`

| Field | Notes |
|-------|--------|
| `quoteId` | Target quote (must be same society, **Draft**) |
| `itemId` | Optional catalog link |
| `description` | Required if no `itemId`; else defaults to item name |
| `quantity` | Must be **> 0** |
| `unitPrice` | If `itemId` set and `unitPrice` ≤ 0, uses catalog default price |
| `discount` | **0–100** (percent) |
| `tvaRate` | Optional; if `itemId` set and null, uses catalog rate |
| `sortOrder` | |

**Response:** `201` with `data` = full **`QuoteDto`** (including `items`).

### `PUT /api/v1/quote-items/{id}` — `UpdateQuoteItemLineRequest`

Same fields as create (except no `quoteId`); replaces the line content. **`itemId`** may be cleared by sending `null` if you still provide a valid **`description`** for a free-text line.

### `DELETE /api/v1/quote-items/{id}`

Deletes the line; response `data` = updated **`QuoteDto`**.

---

## 4) Common error codes (non-exhaustive)

| Code | Typical HTTP | Meaning |
|------|----------------|--------|
| `TENANT_REQUIRED` | 403 | No `society_id` in JWT / context |
| `TENANT_MISMATCH` | 403 | `societyId` query ≠ JWT society |
| `ITEM_NOT_FOUND` | 404 | Catalog item missing or wrong society |
| `QUOTE_NOT_FOUND` | 404 | |
| `QUOTE_ITEM_NOT_FOUND` | 404 | Line id wrong or wrong society |
| `QUOTE_LOCKED` | 409 | Not draft (lines cannot be changed) |
| `VALIDATION_ERROR` | 400 | Bad discount, quantity, missing description, etc. |

---

## 5) Swagger

With the API running, use **`/swagger`** to try requests interactively (authorize with the tenant Bearer token).
