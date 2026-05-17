# Multi-Tenancy & Security

## Architecture

The system uses **SocietyId-based multi-tenancy** with row-level isolation via EF Core global query filters. All tenants share the same PostgreSQL database and schema but data is completely isolated.

---

## Tenant Identification

Every API call carries a signed JWT with the `society_id` claim:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9...
```

The `HttpTenantContext` service extracts `society_id` from the JWT on every request.

---

## Row-Level Isolation

### EF Core Global Query Filters

Every entity inherits `SocietyScopedEntity`:

```csharp
public abstract class SocietyScopedEntity : EntityBase
{
    public Guid SocietyId { get; set; }
}
```

`AppDbContext.OnModelCreating` registers a global query filter for each entity:

```csharp
modelBuilder.Entity<Lead>()
    .HasQueryFilter(x => !x.IsDeleted &&
        (_tenantSocietyId == null || x.SocietyId == _tenantSocietyId));
```

This means **every single query** automatically adds `WHERE SocietyId = @tenantId AND IsDeleted = false` — even if a developer forgets to filter manually.

### Service-Level Filtering

All service methods still explicitly filter by `societyId` as an extra safety layer:

```csharp
var project = await _app.Projects
    .FirstOrDefaultAsync(p => p.Id == projectId && p.SocietyId == societyId);
```

If the project exists but belongs to another tenant → `null` → 404. **No cross-tenant data leakage.**

---

## Authentication Flow

### Tenant JWT (SocietyJwt scheme)

Used by all `/api/v1/*` endpoints (Commercial, Manager, Technician, Admin users).

Claims:
- `sub` — User ID
- `society_id` — Tenant ID
- `role` — User role
- `email` — Email

### Platform JWT (PlatformJwt scheme)

Used by `/api/platform/*` endpoints (SaaS super-admin operations).

---

## Authorization Policies

| Policy | Roles |
|--------|-------|
| `Admin` | Admin |
| `Commercial` | Admin, Commercial |
| `Technician` | Admin, Technician |

All CRM endpoints require authentication with the `TenantJwt` scheme.

---

## Role Permissions

### Admin
- Full access to all data in their society
- Can manage users, roles, settings

### Manager
- Full access to all leads, projects, quotes
- Can assign users
- Can view all financial data

### Commercial
- Sees **only leads and projects assigned to them**
- Can create leads, quotes
- Can mark leads won/lost

### Technician
- Sees **only projects/installations assigned to them**
- Can update installation status
- Cannot see financial data

---

## Cross-Tenant Security Controls

1. **JWT `society_id` claim** — verified on every request
2. **EF Global Query Filter** — automatic WHERE clause injection
3. **Explicit service-level filter** — double-check on every DB query
4. **Optional query param validation** — `TenantCrmControllerBase.ResolveSocietyFromOptionalQuery` rejects mismatched `?societyId=`

```csharp
protected Guid ResolveSocietyFromOptionalQuery(Guid? societyIdFromQuery)
{
    var fromToken = RequireSociety();
    if (societyIdFromQuery is { } q && q != fromToken)
        throw new AppException("TENANT_MISMATCH", "...", 403);
    return fromToken;
}
```

---

## Soft Delete

All entities use soft delete (`IsDeleted = true`) rather than physical deletion. The global query filter excludes deleted records automatically.

This ensures:
- Audit trail is preserved
- Foreign key integrity is maintained
- Accidental data loss is recoverable

---

## Database Indexes for Performance

Every table has:

```sql
CREATE INDEX IX_{Table}_SocietyId ON app."{Table}" ("SocietyId");
```

High-cardinality compound indexes:
```sql
CREATE UNIQUE INDEX IX_Projects_SocietyId_LeadId ON app."Projects" ("SocietyId", "LeadId")
    WHERE "LeadId" IS NOT NULL;

CREATE UNIQUE INDEX IX_Invoices_SocietyId_Reference ON app."Invoices" ("SocietyId", "Reference");
```

---

## Security Checklist for Frontend

- [ ] Always send `Authorization: Bearer {token}` header
- [ ] Never store raw tokens in localStorage (use sessionStorage or secure cookies)
- [ ] Refresh token before expiry
- [ ] Handle `401 Unauthorized` → redirect to login
- [ ] Handle `403 Forbidden` → show "access denied" page
- [ ] Handle `404 Not Found` for any cross-tenant attempt gracefully
- [ ] Never expose `SocietyId` in URL if avoidable — it's in the JWT

---

## Provisioning a New Tenant

1. Platform admin calls `POST /api/platform/societies`
2. `TenantProvisioningService` creates:
   - Society record (platform DB)
   - Default roles (Admin, Manager, Commercial, Technician)
   - Initial admin user
   - Default pipeline stages
   - Default project stages
3. Tenant admin logs in with `POST /api/auth/login`
4. Receives tenant JWT with `society_id` claim
