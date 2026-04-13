# Platform operator guide (full workflow)

Guide for the SaaS owner using **only** `/api/v1/platform/...`. These routes use a **platform JWT** from `POST /api/v1/platform/auth/login`. Tenant CRM routes (`/api/v1/leads`, `/api/v1/users`, …) expect a **tenant JWT** — do not mix tokens.

**Role model (summary)**

| Role | Where | Societies |
|------|--------|-----------|
| **Platform operator** | `platform.PlatformUsers` + platform JWT | Creates/manages societies and plans; **does not** use tenant CRM membership for that identity. |
| **Tenant users** | `core.Users` + tenant JWT | **One** society per user; extra societies are created **here** (platform APIs), not by the tenant admin duplicating orgs. |

Emails registered as **platform users** are excluded from tenant `/users` lists and cannot be invited as tenant users.

## Prerequisites

- API running (e.g. `https://localhost:5001` or `http://localhost:5172`).
- HTTP client (Postman, Insomnia, curl).
- Default platform account (from seed; see `appsettings` / `ACCOUNTS.md`):
  - Email: `plateforme@crm.local`
  - Password: `ChangeMe123!`

## Response shape

Successful calls return JSON like:

```json
{ "success": true, "data": { ... }, "meta": null, "error": null }
```

Errors use `success: false` and `error: { "code", "message", "details" }`.

---

## 1) Smoke: health

`GET /api/health`  
No auth. Expect `{ "status": "ok" }`.

---

## 2) Platform login

`POST /api/v1/platform/auth/login`

```json
{
  "email": "plateforme@crm.local",
  "password": "ChangeMe123!"
}
```

From `data`, copy **`accessToken`**.

**All platform calls below:**

- Header: `Authorization: Bearer <platform_access_token>`

---

## 3) Workflow A — Plans → create society

Use this when you onboard a new customer society from the platform console.

### A.1 List subscription plans

`GET /api/v1/platform/subscription-plans`

Pick a plan **`id`** (GUID) for the next step.

### A.2 Create a society

`POST /api/v1/platform/societies`

```json
{
  "name": "ACME Solar Tunisia",
  "subscriptionPlanId": "PASTE_PLAN_GUID_HERE"
}
```

- `201` with the created society in `data`.
- A subscription row is created for that society (see seed/platform services).

### A.3 List or inspect societies

- `GET /api/v1/platform/societies` — all societies  
- `GET /api/v1/platform/societies/{id}` — one society  

---

## 4) Workflow B — Adjust an existing society

### B.1 Update society (name, active flag, optional plan change)

`PUT /api/v1/platform/societies/{id}`

```json
{
  "name": "ACME Solar Tunisia",
  "isActive": true,
  "subscriptionPlanId": "OPTIONAL_NEW_PLAN_GUID"
}
```

Changing `subscriptionPlanId` supersedes active subscriptions and creates a new subscription window (see platform service behaviour).

### B.2 Delete a society

`DELETE /api/v1/platform/societies/{id}` — platform service applies business rules (e.g. soft delete).

---

## 5) Workflow C — Subscription plans catalog

Manage commercial offers (codes, pricing metadata — see DTOs in code).

| Action | Method | Route |
|--------|--------|--------|
| List | `GET` | `/api/v1/platform/subscription-plans` |
| Create | `POST` | `/api/v1/platform/subscription-plans` |
| Update | `PUT` | `/api/v1/platform/subscription-plans/{id}` |

Use **A.1** after changes to pick plan IDs for new societies.

---

## 6) Workflow D — Edit a subscription row

When you need to change status, end date, or plan on a **subscription entity** (not the society document itself):

`PUT /api/v1/platform/subscriptions/{subscriptionId}`

```json
{
  "status": "Active",
  "endDate": "2027-12-31",
  "planId": "OPTIONAL_PLAN_GUID"
}
```

Get `subscriptionId` from core data, platform DTOs, or your admin UI. `status` is a string (e.g. `Active`, `Superseded` — see domain `SubscriptionStatuses`).

---

## 7) What you cannot do with a platform token

| Action | Result |
|--------|--------|
| `GET /api/v1/auth/me` | **401** — wrong audience / scheme |
| `GET /api/v1/leads` | **401** — tenant routes need tenant JWT |
| Same user as platform **and** tenant | Use **two logins**; platform user is not a tenant user after seed (legacy duplicate email is removed) |

---

## 8) Quick checklist

- [ ] Health OK  
- [ ] Platform login returns `accessToken`  
- [ ] `GET /api/v1/platform/subscription-plans` works  
- [ ] `POST /api/v1/platform/societies` creates a row  
- [ ] `GET /api/v1/platform/societies` lists it  
- [ ] Tenant users complete **their** onboarding with **tenant** login (`guide/TenantSide.md`)

---

## 9) Platform route index

| Area | Routes |
|------|--------|
| Auth | `POST /api/v1/platform/auth/login` |
| Societies | `GET\|POST /api/v1/platform/societies`, `GET\|PUT\|DELETE /api/v1/platform/societies/{id}` |
| Plans | `GET\|POST /api/v1/platform/subscription-plans`, `PUT /api/v1/platform/subscription-plans/{id}` |
| Subscriptions | `PUT /api/v1/platform/subscriptions/{id}` |

Swagger: `/swagger` when the API exposes it.
