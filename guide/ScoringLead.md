# Scoring lead (LVI / SD) — intégration front

## Production (API)

Le scoring est **intégré aux leads** : les scores sont **calculés et stockés** sur la table `app."Leads"` (`lvi`, `sd`, `scoredAt`, colonnes `scoreBreakdown*`). Le recalcul a lieu après **création**, **mise à jour**, **activité**, **assignation**, **conversion**, **gagné/perdu**. **`POST /api/v1/leads/{id}/score`** force un recalcul.

- **Qualification** : champs sur le lead (`monthlyBillEur`, `estimatedKw`, `averageRating`, bonus booléens) via `POST` / `PUT /api/v1/leads`.
- **Comportement** : agrégation des `LeadActivities` dont le `type` correspond à `LeadActivityTypes` dans le domaine (ex. `Call`, `WhatsApp`, `QuoteRequest`, `MeetingScheduled`).
- **Liste** : `GET /api/v1/leads?sortBy=lvi&sortOrder=desc` pour trier par LVI.

---

## Référence moteur (contrat logique)

Le moteur utilise `LeadScoringInput` / `ScoreResult` (`CrmPhotoVolta.Application.Scoring`). Les exemples JSON ci‑dessous servent à comprendre les **axes** ou pour **tests unitaires** ; en production le front consomme surtout **`GET /api/v1/leads/{id}`** (objet lead avec `lvi`, `sd`, `scoreBreakdown`).

Les pondérations par défaut sont `LeadScoringWeights.Default` (interaction 0,25 ; intention 0,30 ; satisfaction 0,15 ; activité 0,10 ; potentiel 0,20 ; facteur SD sur satisfaction 0,5).

---

## Corps de requête (JSON recommandé, `camelCase`)

| Champ | Type | Rôle |
|--------|------|------|
| `interactions` | objet | Compteurs d’événements |
| `interactions.calls` | entier | Appels |
| `interactions.whatsAppSms` | entier | WhatsApp / SMS |
| `interactions.meetingsScheduled` | entier | Rendez-vous planifiés |
| `interactions.technicalVisits` | entier | Visites techniques |
| `intentions` | objet | Signaux d’intention (sommes par catégorie) |
| `intentions.infoRequests` | entier | Demandes d’info |
| `intentions.quoteRequests` | entier | Demandes de devis |
| `intentions.negotiations` | entier | Négociations |
| `intentions.strongBuyingSignals` | entier | Signaux d’achat forts |
| `averageRating` | nombre | Note moyenne **1–5** ; mettre **0** si inconnu (satisfaction = 0) |
| `timeSinceLastActivity` | nombre \| null | **Secondes** écoulées depuis la dernière activité utile ; **`null`** = inconnu (activité 0, **aucune** pénalité temporelle) |
| `monthlyBill` | nombre | Facture mensuelle (€), ≥ 0 |
| `estimatedKw` | nombre | Puissance estimée (kWc), ≥ 0 |
| `bonuses` | objet | Bonus booléens (devis demandé, budget, décideur, financement) |

**Note front :** si le backend expose un jour un endpoint, mappez `timeSinceLastActivity` depuis une durée ISO 8601 (`PT12H`, `P2D`) ou des secondes — à valider dans l’OpenAPI.

---

## Réponse attendue (forme cible)

```json
{
  "lvi": 58.25,
  "sd": 98.25,
  "breakdown": {
    "interaction": 35,
    "intention": 25,
    "satisfaction": 80,
    "activity": 100,
    "potential": 100,
    "penalties": 0
  }
}
```

- **LVI** : score composite 0–100 (avec pénalité d’inactivité soustraite).
- **SD** : `LVI + 0,5 × satisfaction` (plafonné 0–100).
- **breakdown** : sous-scores bruts 0–100 par axe (les **pénalités** sont un **montant positif** retiré du LVI, pas un axe négatif).

---

## Scénario 1 — Lead « chaud »

Entrée type :

```json
{
  "interactions": {
    "calls": 2,
    "whatsAppSms": 1,
    "meetingsScheduled": 1,
    "technicalVisits": 0
  },
  "intentions": {
    "infoRequests": 1,
    "quoteRequests": 1,
    "negotiations": 0,
    "strongBuyingSignals": 0
  },
  "averageRating": 4,
  "timeSinceLastActivity": 43200,
  "monthlyBill": 150,
  "estimatedKw": 50,
  "bonuses": {
    "quoteRequested": true,
    "budgetConfirmed": true,
    "decisionMaker": true,
    "financingInterest": true
  }
}
```

`43200` = 12 h en secondes → activité maximale, pas de pénalité.

**Résultat attendu (poids par défaut) :** `lvi` ≈ **58,25**, `sd` ≈ **98,25** (voir `breakdown` dans l’exemple de réponse ci-dessus pour ce jeu de données).

---

## Scénario 2 — Lead « froid » (longue inactivité)

```json
{
  "interactions": { "calls": 0, "whatsAppSms": 0, "meetingsScheduled": 0, "technicalVisits": 0 },
  "intentions": { "infoRequests": 0, "quoteRequests": 0, "negotiations": 0, "strongBuyingSignals": 0 },
  "averageRating": 0,
  "timeSinceLastActivity": 2160000,
  "monthlyBill": 0,
  "estimatedKw": 0,
  "bonuses": {
    "quoteRequested": false,
    "budgetConfirmed": false,
    "decisionMaker": false,
    "financingInterest": false
  }
}
```

`2160000` s ≈ **25 jours** → pénalité maximale (30 points), activité nulle.

**Résultat attendu :** `lvi` = **0**, `sd` = **0**, `breakdown.penalties` = **30**.

---

## Scénario 3 — Lead « moyen » (référence rapide)

```json
{
  "interactions": { "calls": 1, "whatsAppSms": 0, "meetingsScheduled": 0, "technicalVisits": 0 },
  "intentions": { "infoRequests": 0, "quoteRequests": 1, "negotiations": 0, "strongBuyingSignals": 0 },
  "averageRating": 3,
  "timeSinceLastActivity": 172800,
  "monthlyBill": 80,
  "estimatedKw": 8,
  "bonuses": {
    "quoteRequested": false,
    "budgetConfirmed": false,
    "decisionMaker": false,
    "financingInterest": false
  }
}
```

`172800` s = **2 jours** → pas de pénalité ; activité dans la tranche « 24 h–3 j ».

**Résultat attendu (arrondi) :** `lvi` ≈ **36,97**, `sd` ≈ **66,97**.

---

## Intégration front (résumé)

1. **Constituer le corps** à partir du CRM (historique d’appels, intentions agrégées, dernière activité → secondes, facture / kWc, cases bonus).
2. **Afficher** LVI et SD (barres, badges) + optionnellement le détail `breakdown` pour un tooltip « pourquoi ce score ».
3. **Cas limite :** `timeSinceLastActivity: null` si la date est inconnue — pas de pénalité, score d’activité à 0.
4. En intégration réelle, lire **`lvi` / `sd` / `scoreBreakdown`** sur le DTO lead ; les scénarios ci-dessous restent utiles pour **tests** ou compréhension du moteur.

Pour le détail des formules et constantes (points par appel, paliers de pénalité jours, etc.), voir `LeadScoringService.cs` dans le dépôt.
