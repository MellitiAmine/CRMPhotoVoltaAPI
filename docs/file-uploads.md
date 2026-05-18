# File uploads — frontend integration summary

> **Storage:** local `wwwroot/files` (config: `FileStorage` in appsettings).  
> **Future:** set `FileStorage:Provider` to `Cloudinary` when implemented.

---

## Breaking change

**Do not send JSON with a `url` field anymore.** Upload files with **`multipart/form-data`** and field name **`file`**.

The API stores the file on disk and returns a **`url`** you use in `<img src>` / download links.

---

## URL format

| Config | Example |
|--------|---------|
| Relative (default) | `/files/{societyId}/installations/{installationId}/photos/{guid}.jpg` |
| With `PublicBaseUrl` | `https://api.example.com/files/...` |

Same host as the API → use `url` as-is.  
Separate CDN/domain → set `FileStorage:PublicBaseUrl` in appsettings.

---

## Installation photos

| Action | Method | Endpoint | Body |
|--------|--------|----------|------|
| List | GET | `/api/v1/installations/{id}/photos` | — |
| Upload | POST | `/api/v1/installations/{id}/photos` | `multipart`: `file` (image) |
| Delete | DELETE | `/api/v1/installations/{id}/photos/{photoId}` | — |

**Allowed images:** `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.heic`  
**Max size:** 10 MB (configurable)

**Response item:**
```json
{
  "id": "uuid",
  "url": "/files/.../photo.jpg",
  "fileName": "abc123.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245000,
  "uploadedAt": "2026-05-18T10:00:00Z"
}
```

`GET /api/v1/installations/{id}` still includes `photos[]` with the same shape.

### Angular example

```typescript
const form = new FormData();
form.append('file', file, file.name);

this.http.post(`/api/v1/installations/${id}/photos`, form, {
  headers: { /* no Content-Type — browser sets boundary */ }
});
```

---

## Project documents

| Action | Method | Endpoint | Body |
|--------|--------|----------|------|
| List | GET | `/api/v1/projects/{id}/documents` | — |
| Upload | POST | `/api/v1/projects/{id}/documents` | `multipart`: `file`, `type` (enum), optional `name` |
| Delete | DELETE | `/api/v1/projects/{id}/documents/{documentId}` | — |

**Form fields:**

| Field | Required | Notes |
|-------|----------|--------|
| `file` | yes | PDF, Office, images (see appsettings) |
| `type` | yes | `ProjectDocumentType` enum (e.g. `Contract`, `Technical`, `Other`) |
| `name` | no | Display name; defaults to file name without extension |

**Response:** same pattern as photos (`url`, `fileName`, `contentType`, `sizeBytes`, plus `type`, `name`, `uploadedByName`).

---

## Generic documents (client / project registry)

| Action | Method | Endpoint |
|--------|--------|----------|
| Upload | POST | `/api/v1/documents/upload` |
| List by project | GET | `/api/v1/documents/projects/{projectId}` |
| List by client | GET | `/api/v1/documents/clients/{clientId}` |

**Upload form:** `file`, optional `projectId`, `clientId`, `type` (string label).

---

## appsettings (`FileStorage`)

```json
{
  "FileStorage": {
    "Provider": "Local",
    "WebRootPath": "wwwroot",
    "PublicPathPrefix": "/files",
    "PublicBaseUrl": "",
    "MaxFileSizeBytes": 10485760
  }
}
```

For production behind a reverse proxy, set `PublicBaseUrl` to the public API URL so mobile apps get absolute links.

---

## Errors

| Code | When |
|------|------|
| `VALIDATION_ERROR` | Missing file, wrong extension, file too large |
| `PHOTO_NOT_FOUND` | Bad installation photo id |
| `DOCUMENT_NOT_FOUND` | Bad project document id |
| `INSTALLATION_NOT_FOUND` | Bad installation id |
| `PROJECT_NOT_FOUND` | Bad project id |

---

## Migration note

Old uploads under `/uploads/...` are **not** migrated automatically. New uploads go to `/files/...`. Re-upload or keep legacy URLs if still served.
