## Rescue Team Complete Flow Update

Date: 2026-03-24

### Scope

This update applies the smallest possible BE change to fix the current Rescue Team "Hoan tat" flow without changing DB schema or DB constraints.

Touched code:

- `API/Controllers/RescueTeamController.cs`

No DB change was made.

### Problem

Current FE calls:

- `PUT /api/rescue-team/operations/{operationId}/status`

When Rescue Team pressed "Hoan tat", BE completed the operation but also inserted a new row into `rescue_request_status_history` using the current request status.

In the current flow, the request status remains `Assigned` until Citizen/Guest presses "Bao an toan".

Because the DB already has a unique index by `(request_id, status)`, inserting another `Assigned` history row caused:

- `DbUpdateException`
- unique index `UX_rrsh_request_status`
- HTTP `500`

### Expected Flow

1. Coordinator verifies request.
2. Coordinator assigns team.
3. Rescue Team presses "Hoan tat":
   - `rescue_operations.status` -> `Completed`
   - `rescue_requests.status` stays `Assigned`
   - Citizen/Guest becomes eligible to press "Bao an toan"
4. Citizen/Guest presses "Bao an toan":
   - `rescue_requests.status` -> `Completed`

### Applied Fix

For `PUT /api/rescue-team/operations/{operationId}/status`:

- Keep request status as `Assigned` when Rescue Team sends `COMPLETED`
- Do not insert duplicate `Assigned` history into `rescue_request_status_history`
- Return a business response message that clearly states:
  - Rescue Team finished the mission
  - request is still `Assigned`
  - Citizen can now confirm safety

### Validation Added

The endpoint now validates:

- request body must exist
- JWT user id must be valid
- operation must exist
- linked request must exist
- caller must be an active member of the assigned team
- operation must still be in `Assigned`
- linked request must still be in `Assigned` before Rescue Team can complete

### Error Handling Added

The endpoint now returns controlled API errors instead of raw `500` for:

- concurrency conflict
- duplicate request-status-history conflict
- generic DB save failure

### Notes

- `PUT /api/RescueRequest/{id}/confirm-rescued` and
  `PUT /api/RescueRequest/guest/{id}/confirm-rescued`
  remain the final step that changes request status to `Completed`.
- This update intentionally avoids wider refactor to reduce regression risk.
