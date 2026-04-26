# Leads API & Workflow Docs

This document provides guidelines for frontend developers and AI agents interacting with the Leads API of the CRM PhotoVolta platform.

## Context & Business Rules (LVI / SD Scores)
The CRM relies on two crucial metrics for organizing leads:
- **LVI (Lead Value Index):** Measures value and engagement (interest, quality, budget).
- **SD (Score Décisionnel):** Decision score. This drives what action the commercial should take immediately.
  - `SD >= 85`: Priority/Urgent call.
  - `SD 70-84`: Call soon.
  - `SD 50-69`: Message (WhatsApp).
  - `SD < 50`: Nurturing / marketing.

The frontend should leverage the priorities (`Urgent`, `High`, `Low`) and temperatures derived from the scores (`Hot`, `High`, `Medium`, `Low`, `Cold`) to render visual priorities. Lead statues typically flow as: `Nouveau` -> `Contacté` -> `Qualifié` -> `Proposition` -> `Négociation` -> `Gagné`/`Perdu` -> `Archivé` -> `Installation`.

## Leads Endpoint Group
**Base Path:** `/api/v1/leads`
**Headers:** `Authorization: Bearer <tenant_accessToken>`

### Core CRUD Operations
1. **List Leads:** `GET /`
   - Supports Query parameters for pagination (`page`, `pageSize`, `search`, `sortBy=lvi`, `sortOrder`).
   - Returns paginated `LeadListItemDto` items.

2. **Create Lead:** `POST /`
   - **Request Payload (`CreateLeadRequest`):**
   ```json
   {
     "name": "Acme Corp",
     "phone": "123456789",
     "email": "contact@acme.com",
     "address": "123 Main St",
     "status": "Nouveau", // optional string
     "assignedToUserId": "uuid-here", // optional
     "monthlyBillEur": 1500, // optional
     "estimatedKw": 10, // optional
     "averageRating": 0, // optional
     "bonusQuoteRequested": false, // optional boolean
     "bonusBudgetConfirmed": false, // optional boolean
     "bonusDecisionMaker": true, // optional boolean
     "bonusFinancingInterest": false // optional boolean
   }
   ```
   - Yields HTTP `201 Created` with the fully created `LeadDto` (which includes `lvi`, `sd`, `temperature`, `priority`, etc).

3. **Get Lead:** `GET /{id}`
   - Returns `LeadDto` including `lvi`, `sd`, `scoredAt`, and a detailed `ScoreBreakdown` object (`interaction`, `intention`, `satisfaction`, `activity`, `potential`, `penalties`).

4. **Update Lead:** `PUT /{id}`
   - Payload (`UpdateLeadRequest`) is identical structurally to `CreateLeadRequest` but requires `status` to not be null.

5. **Delete Lead:** `DELETE /{id}`

### Progression & Conversion
- **Assign to Commercial:** `POST /{id}/assign`
  ```json
  { "userId": "uuid-of-commercial" }
  ```
- **Convert to Client:** `POST /{id}/convert`
  ```json
  { 
    "createDeal": true,
    "dealTitle": "Acme Corp Solar Project" // Optional 
  }
  ```
  Returns `ConvertLeadResultDto` (`lead`, `clientId`, `dealId`).

- **Mark Outcomes:** 
  - `POST /{id}/mark-won` (no payload)
  - `POST /{id}/mark-lost` (no payload)

### Interactions & Tracking
It is **CRITICAL** that end-users populate these correctly for the AI scoring engine to remain accurate.

- **Add Activity:** `POST /{id}/activities`
  ```json
  {
    "type": "Call", // Enum/String map: Call, QuoteRequest, MeetingScheduled, Whatsapp, etc.
    "notes": "Discussed initial budget.", // optional
    "rating": 5 // optional, out of 5
  }
  ```
- **List Activities:** `GET /{id}/activities` Returns list of `LeadActivityDto`.

- **Add Note:** `POST /{id}/notes`
  ```json
  { "body": "Need to follow up about roof dimensions next week." }
  ```

- **Timeline:** `GET /{id}/timeline` 
  Returns a mixed list of `LeadTimelineEntryDto` representing a chronology of notes, activities, and status changes.

### Utility
- **Recalculate Score Manually:** `POST /{id}/score`
  Forces recalculation of LVI/SD (similar to what happens automatically upon activity/update) and returns `LeadDto`.
