# Tenant Authentication API Docs

This document is designed to help frontend developers and AI agents integrate with the Tenant Auth flow of the CRM PhotoVolta APIs.

## Context
Tenant users are members of a specific society (admin, commercial, technician). They must authenticate using the tenant login endpoint. The resulting JWT naturally embeds the `society_id`. There is no need to pass a separate `society_id` header in subsequent requests to tenant APIs (routes under `/api/v1/...` excluding `/api/v1/platform/...`).

## 1. Login
**Endpoint:** `POST /api/v1/auth/login` (Anonymous)

**Request Payload (`LoginRequest`):**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response Data (`AuthTokensResponse`):**
The API wraps its response inside an `ApiResponse` object (`data` property).
```json
{
  "status": 200,
  "data": {
    "accessToken": "ey...",
    "refreshToken": "...",
    "accessTokenExpiresAt": "2024-12-31T23:59:59Z",
    "userId": "uuid-here",
    "societyId": "uuid-here", // Embedded society
    "role": "Admin" // Example: Admin, Commercial, Technician
  }
}
```

**How to store/use:**
- Store `accessToken` securely. Pass it as a header for all subsequent tenant requests: `Authorization: Bearer <accessToken>`.
- Keep `refreshToken` to auto-renew sessions via `POST /api/v1/auth/refresh`.

## 2. Get Current User / Context
**Endpoint:** `GET /api/v1/auth/me` (Protected)
**Headers:** `Authorization: Bearer <accessToken>`

**Purpose:** Fetches user profile and their **current** society context (since one account = one organization).

**Response Data (`MeResponse`):**
```json
{
  "status": 200,
  "data": {
    "userId": "uuid-here",
    "email": "user@example.com",
    "fullName": "John Doe",
    "phone": "+33 6 12 34 56 78",
    "currentSocietyId": "uuid-here",
    "societies": [
      {
        "societyId": "uuid-here",
        "societyName": "My Awesome Solar Co",
        "roleId": "uuid-here",
        "roleName": "Admin"
      }
    ]
  }
}
```

## 3. Other Utility Endpoints
- **Register (if enabled):** `POST /api/v1/auth/register`
  - Payload (`RegisterRequest`): `{ "email": "", "password": "", "fullName": "", "phone": "", "societyName": "" }`
- **Refresh Token:** `POST /api/v1/auth/refresh`
  - Payload (`RefreshRequest`): `{ "refreshToken": "<refresh_token>" }`
- **Logout:** `POST /api/v1/auth/logout`
  - Payload (`RefreshRequest`): `{ "refreshToken": "<refresh_token>" }`

## Architecture Notes
- Using platform tokens for tenant routes will yield a `401 Unauthorized`. 
- Attempting to access cross-society IDs or if token is missing `society_id` yields `403 Forbidden` (`TENANT_REQUIRED`).
- No `<domain>/auth/switch-society` exists. One user account strictly belongs to a single tenant society.
