# Installation Planning — Techniciens (API & Frontend)

> **Base URL:** `/api/v1/installations`  
> **Auth:** Bearer JWT (`TenantJwt`, `society_id` required)  
> **Angular route suggérée:** `/planning` ou `/installations/planning`

---

## Objectif

Afficher un **planning dynamique** des installations terrain avec une visibilité différente selon le rôle :

| Rôle | Endpoint | Visibilité |
|------|----------|------------|
| **Admin / Manager** | `GET /installations/planning` | Tous les techniciens, filtre optionnel |
| **Technicien** (et tout utilisateur) | `GET /installations/my-planning` | Uniquement ses installations assignées |

---

## Logique frontend (important)

```typescript
// Au chargement du module planning
const role = authService.currentRole; // depuis JWT / profil

if (role === 'Admin' || role === 'Manager') {
  this.http.get('/api/v1/installations/planning', { params: { from, to, technicianId } });
} else {
  this.http.get('/api/v1/installations/my-planning', { params: { from, to } });
}
```

Ne pas appeler `/planning` pour un technicien — le backend renvoie **403 Forbidden**.

---

## Endpoints

### `GET /api/v1/installations/planning` (Admin / Manager)

Planning global de la société.

**Query parameters**

| Param | Type | Description |
|-------|------|-------------|
| `from` | date (`YYYY-MM-DD`) | Début de période (défaut : aujourd'hui − 7 jours) |
| `to` | date | Fin de période (défaut : aujourd'hui + 30 jours) |
| `technicianId` | uuid | Filtre sur un technicien |
| `status` | enum | `Scheduled`, `InProgress`, `Completed`, `Cancelled` |

**Exemple**

```http
GET /api/v1/installations/planning?from=2026-07-01&to=2026-07-31&technicianId=uuid
```

---

### `GET /api/v1/installations/my-planning` (Technicien)

Installations où `TechnicianId` = utilisateur connecté (`sub` du JWT).

**Query parameters:** `from`, `to`, `status` (pas de `technicianId` — forcé côté serveur).

**Exemple**

```http
GET /api/v1/installations/my-planning?from=2026-07-01&to=2026-07-31
```

---

## Réponse (même structure pour les deux)

```json
{
  "success": true,
  "data": {
    "from": "2026-07-01",
    "to": "2026-07-31",
    "isPersonalView": false,
    "technicianId": null,
    "items": [
      {
        "id": "uuid",
        "projectId": "uuid",
        "projectReference": "PRJ-2026-0042",
        "projectName": "Installation 6 kWc — Villa Dupont",
        "clientName": "Famille Dupont",
        "address": "12 Rue Victor Hugo, Tunis",
        "technicianId": "uuid",
        "technicianName": "Lucas Bernard",
        "date": "2026-07-15",
        "status": "Scheduled",
        "checklistCompleted": 0,
        "checklistTotal": 6,
        "createdAt": "2026-06-01T09:00:00Z",
        "updatedAt": null
      }
    ],
    "technicians": [
      {
        "technicianId": "uuid",
        "technicianName": "Lucas Bernard",
        "totalCount": 5,
        "scheduledCount": 2,
        "inProgressCount": 1,
        "completedCount": 2
      }
    ]
  }
}
```

| Champ | Description |
|-------|-------------|
| `isPersonalView` | `true` pour `/my-planning` |
| `technicianId` | Filtre actif (null = tous) |
| `items` | Événements planning (une ligne = une installation à la date `date`) |
| `technicians` | Résumé par technicien (**vide** en vue personnelle) |

---

## UI Angular suggérée

### Vue Admin

- Calendrier / timeline multi-colonnes (une colonne par technicien) ou liste groupée
- Filtre dropdown alimenté par `technicians[]`
- Clic item → `/installations/{id}`
- Couleur par `status` :
  - `Scheduled` → bleu
  - `InProgress` → orange
  - `Completed` → vert
  - `Cancelled` → gris

### Vue Technicien

- Liste ou calendrier personnel (une seule ressource)
- Afficher : date, client, projet, adresse, progression checklist
- Actions : démarrer (`POST /installations/{id}/start`), checklist, photos

### Composant partagé

```typescript
interface PlanningViewModel {
  from: string;
  to: string;
  items: InstallationPlanningItem[];
  technicians: TechnicianPlanningSummary[]; // vide si technicien
  isPersonalView: boolean;
}
```

---

## Mapping statut installation

| `status` | Label FR |
|----------|----------|
| `Scheduled` | Planifiée |
| `InProgress` | En cours |
| `Completed` | Terminée |
| `Cancelled` | Annulée |

---

## Erreurs

| Code | HTTP | When |
|------|------|------|
| `FORBIDDEN` | 403 | Technicien appelle `/planning` |
| `UNAUTHORIZED` | 401 | JWT manquant |
| `VALIDATION_ERROR` | 400 | `to` &lt; `from` |
| `TENANT_REQUIRED` | 403 | Pas de `society_id` |

---

## Relation avec autres APIs

| Besoin | Endpoint |
|--------|----------|
| Détail installation | `GET /installations/{id}` |
| Espace technicien (liste simple) | `GET /me/installations` |
| Calendrier événements | `GET /calendar` |
| Techniciens (profils RH) | `GET /techniciens` |

---

## Période par défaut

Si `from` / `to` sont omis :

- **from** = aujourd'hui (UTC) − 7 jours  
- **to** = aujourd'hui (UTC) + 30 jours  

Adapter les query params au changement de vue (semaine / mois) côté Angular.
