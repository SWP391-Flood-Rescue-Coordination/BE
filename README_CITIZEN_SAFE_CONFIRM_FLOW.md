# Citizen Safe Confirmation Flow

Date: 2026-03-25

## Purpose

This document describes the current implementation of the rescue completion and citizen safe-confirmation flow across both Backend and Frontend.

The main business goal is:

1. The Rescue Team can mark its mission as completed.
2. The rescue request must remain in `Assigned` at that moment.
3. The Citizen or Guest is the final actor who closes the request by pressing `Report Safe`.
4. Only after that final confirmation does the request move to `Completed`.

## Scope

### Backend files

- `API/Controllers/RescueTeamController.cs`
- `API/Controllers/RescueRequestController.cs`
- `API/DTOs/RescueRequestDto.cs`

### Frontend files

- `src/components/Dashboard.jsx`
- `src/components/ViewRequest.jsx`
- `src/components/ViewRequest.css`
- `src/components/RequestForm.jsx`
- `src/services/rescueRequestService.js`

## Flow Summary

### Step 1: Coordinator verifies and assigns the request

This is the normal pre-condition for the flow.

- Request status becomes `Assigned`.
- A rescue operation is linked to the request.

### Step 2: Rescue Team presses Complete

Frontend calls:

- `PUT /api/rescue-team/operations/{operationId}/status`

Payload:

```json
{
  "newStatus": "COMPLETED"
}
```

Backend behavior:

- Validates the request body.
- Validates the JWT user id.
- Validates that the caller is an active member of the assigned team.
- Validates that the operation is still in `Assigned`.
- Validates that the linked rescue request is still in `Assigned`.
- Updates `rescue_operations.status` to `Completed`.
- Sets `StartedAt` if it was null.
- Sets `CompletedAt`.
- Keeps `rescue_requests.status` unchanged as `Assigned`.
- Returns the team to `AVAILABLE`.
- Returns all linked vehicles to `AVAILABLE`.
- Does not insert a duplicate `Assigned` row into `rescue_request_status_history`.

Business result:

- The team has finished its work.
- The citizen request is not closed yet.
- The citizen or guest becomes eligible to press `Report Safe`.

## Step 3: Backend exposes a computed flag for the UI

The rescue request retrieval APIs now expose a computed field:

- `CanReportSafe`

This flag is `true` only when:

1. The rescue request status is still `Assigned`.
2. At least one linked rescue operation has already been completed.

This flag is returned through the request DTOs used by:

- `GET /api/RescueRequest/my-requests`
- `GET /api/RescueRequest/my-latest-request`
- `GET /api/RescueRequest/{id}`
- `GET /api/RescueRequest/guest/status`

Important note:

- `CanReportSafe` is computed at runtime.
- It is not stored as a physical column in the database.

## Step 4: Citizen presses Report Safe

Frontend calls:

- `PUT /api/RescueRequest/{id}/confirm-rescued`

Authentication:

- Required
- Role: `CITIZEN`

Backend behavior:

- Verifies ownership of the request.
- Verifies that the request status is `Assigned`.
- Verifies that at least one linked operation is `Completed`.
- Changes the request status to `Completed`.
- Writes a `Completed` row into `rescue_request_status_history`.
- Uses the authenticated citizen id in `updated_by`.

Response result:

- The request is closed.
- The UI should stop showing the safe-confirmation notice.

## Step 5: Guest presses Report Safe

Frontend calls:

- `PUT /api/RescueRequest/guest/{id}/confirm-rescued`

Payload:

```json
{
  "phone": "0912345678"
}
```

Authentication:

- Not required

Backend behavior:

- Requires `phone`.
- Loads the request by id.
- Verifies that the submitted phone matches `request.Phone` or `request.ContactPhone`.
- Verifies that the request status is `Assigned`.
- Verifies that at least one linked operation is `Completed`.
- Changes the request status to `Completed`.
- Writes a `Completed` row into `rescue_request_status_history`.
- Uses `updated_by = -1` to represent Guest/System without a real user account.

Response result:

- The request is closed.
- The guest flow matches the citizen closing behavior.

## Database Prerequisite for Guest Confirmation

Because guest confirmation uses `updated_by = -1`, the following foreign keys must not exist anymore:

- `FK_rrsh_updated_by`
- `FK_rescue_requests_updated_by`

The required SQL change is to drop those foreign keys so that Guest/System actions can be persisted without a real `users.user_id`.

This flow does not require any new tables or new columns.

## Frontend Behavior

### Citizen dashboard

When `latestRequest.canReportSafe === true` and the request status is still `Assigned`:

- The main request button switches to a green ready state.
- The `View Request` button shows a red notification dot.
- A floating notice appears on the dashboard.
- The notice includes a direct `Report Safe` action.

If the user confirms safety from either place:

- the floating notice disappears,
- the red dot disappears,
- the main button returns to its normal `Create Request` state,
- the request is considered closed.

### View Request modal

The View Request screen now matches the Create Request layout:

- Left column: phone, location, map, address
- Right column: people counts, conditions, notes

The modal behavior is:

- `Report Safe` is visible in the action area.
- It is enabled only when `CanReportSafe` is true.
- Edit is only allowed while the request is still `Pending`.

## Validation Rules

### Rescue Team complete API

- Body must exist.
- `NewStatus` must be valid.
- Caller must belong to the assigned team.
- Operation must still be `Assigned`.
- Linked request must still be `Assigned`.

### Citizen confirm API

- Caller must own the request.
- Request must still be `Assigned`.
- A completed operation must already exist.

### Guest confirm API

- `phone` is required.
- Phone must match the request.
- Request must still be `Assigned`.
- A completed operation must already exist.

### Frontend form rules

- Phone validation follows the same Vietnamese phone rules used in the request form.
- `Total People >= Elderly + Children`
- View Request layout and the Create Request layout are intentionally aligned to reduce editing mistakes.

## Why This Flow Was Changed

The older logic had a business conflict:

- Rescue Team completed the mission.
- The request immediately became `Completed`.
- Citizen/Guest then could no longer press `Report Safe`.

The new flow fixes that by separating:

1. Team mission completion
2. Citizen/Guest safety acknowledgment

This gives a clearer end-user experience and a cleaner business narrative for demos and review.

## API Contract Overview

### Rescue Team completion

- Method: `PUT`
- Route: `/api/rescue-team/operations/{operationId}/status`
- Main payload for this flow:

```json
{
  "newStatus": "COMPLETED"
}
```

### Citizen safe confirmation

- Method: `PUT`
- Route: `/api/RescueRequest/{id}/confirm-rescued`
- No request body needed

### Guest safe confirmation

- Method: `PUT`
- Route: `/api/RescueRequest/guest/{id}/confirm-rescued`

```json
{
  "phone": "0912345678"
}
```

## Notes

- The current Rescue Team endpoint still contains a `FAILED` branch for legacy handling, but the safe-confirmation flow described in this document is specifically about the `COMPLETED` path.
- The UI behavior depends on `CanReportSafe`, not on a separate database flag.
- The frontend and backend are intentionally coordinated so that the request stays visually and logically in `Assigned` until the user explicitly confirms safety.
