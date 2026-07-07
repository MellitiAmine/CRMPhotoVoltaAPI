# Profile identity deduplication — Technicien & Commercial

> **Last updated:** 2026-07-07  
> **Scope:** `TechnicienProfiles`, `CommercialProfiles`, `core.Users`

---

## Problem

HR profile tables (`TechnicienProfiles`, `CommercialProfiles`) duplicated identity fields already stored on `core.Users` (`FullName`, `Email`, `Phone`). On profile update, only the profile row changed while `Users` stayed stale. The rest of the app (installations, planning, leads, etc.) reads names from `Users`, causing inconsistent display.

---

## Solution

**`core.Users` is the single source of truth** for identity/contact fields. Profile tables keep only HR/performance data linked via `UserId`.

| Removed from profile tables | Stored on `core.Users` | Exposed in API DTOs |
|----------------------------|------------------------|---------------------|
| `FirstName`, `LastName` | `FullName` (split on first space for DTOs) | `firstName`, `lastName` |
| `Email` | `Email` | `email` |
| `Phone` | `Phone` | `phone` |

**Still on profile tables:** `AvatarUrl`, `DateOfBirth`, `Address`, `City`, employment fields, KPIs, scores, attendance, emergency contact.

---

## Behaviour changes

### Create (`POST /techniciens`, `POST /commercials`)

1. Resolve or create `core.Users` (by `userId` or email).
2. Write `firstName`, `lastName`, `phone` → `Users.FullName` / `Users.Phone`.
3. Write `email` → `Users.Email` when not linking an existing user by `userId`.
4. Insert profile with HR fields only.

### Update (`PUT /techniciens/{id}`, `PUT /commercials/{id}`)

- `firstName`, `lastName`, `phone` → update `core.Users` (not the profile table).
- Other fields (department, salary, status, etc.) → update profile as before.

### Read (list, detail, stats)

- DTOs unchanged for clients: identity is loaded from `Users` via `UserId`.
- Search filters name/email/phone against `Users`, plus profile fields (`employeeId`, `position`, `department`).

---

## Migrations

### Apply

**Terminal (recommended)** — run from the solution root (`D:\Desktop\CrmPhotoVoltaApis`):

```powershell
dotnet ef database update --context AppDbContext `
  --project CrmPhotoVolta.Infrastructure\CrmPhotoVolta.Infrastructure.csproj `
  --startup-project CrmPhotoVoltaApis.csproj
```

> **Note:** The API project is `CrmPhotoVoltaApis.csproj` at the repo root (not a `CrmPhotoVoltaApis\` subfolder). Use the `.csproj` extension in CLI commands.

**Package Manager Console (Visual Studio):**

1. Set **Startup Project** → `CrmPhotoVoltaApis`
2. Set **Default project** (PMC dropdown) → `CrmPhotoVolta.Infrastructure`
3. Stop debugging / close any running API instance (avoids file-lock build errors)
4. Run:

```powershell
Update-Database -Context AppDbContext -Project CrmPhotoVolta.Infrastructure -StartupProject CrmPhotoVoltaApis
```

Or:

```powershell
dotnet ef database update --context AppDbContext --project CrmPhotoVolta.Infrastructure\CrmPhotoVolta.Infrastructure.csproj --startup-project CrmPhotoVoltaApis.csproj
```

If PMC reports `Build failed` but `dotnet build` succeeds, stop the debugger, close IIS Express, then **Build → Rebuild Solution** and retry.

### Verify

```powershell
dotnet ef migrations list --context AppDbContext `
  --project CrmPhotoVolta.Infrastructure\CrmPhotoVolta.Infrastructure.csproj `
  --startup-project CrmPhotoVoltaApis.csproj
```

| Migration | Table | Action |
|-----------|-------|--------|
| `20260707150000_RemoveTechnicienProfileIdentityDuplicates` | `app.TechnicienProfiles` | Sync existing identity → `core.Users`, drop `FirstName`, `LastName`, `Email`, `Phone` |
| `20260707151000_RemoveCommercialProfileIdentityDuplicates` | `app.CommercialProfiles` | Same for commercials |

Each migration runs a one-time SQL sync **before** dropping columns so the latest profile values are preserved on `Users`.

---

## Code changes

| Area | Files |
|------|-------|
| Domain | `TechnicienProfile.cs`, `CommercialProfile.cs` |
| EF config | `AppDbContext.cs` |
| Services | `TechnicienService.cs`, `CommercialService.cs` |
| Migrations | `20260707150000_…`, `20260707151000_…` |

API request/response shapes (`TechnicienDtos`, `CommercialDtos`) are **unchanged** — only persistence and sync logic moved.

---

## Impact on the rest of the app

Anything resolving display names via `Installation.TechnicianId → Users.FullName` or similar user joins now stays in sync when a technicien/commercial profile is edited.

No frontend contract change required unless the UI was reading raw DB columns directly (it should use the API DTOs).

---

## Related docs

- [technicien.md](./technicien.md) — Technicien API reference
- [commercial.md](./commercial.md) — Commercial API reference
