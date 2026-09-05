# User management API

## Goal

Add permission-protected APIs for administrators to list, filter, retrieve, create, update, and deactivate users.

## Endpoints

- `GET /api/v1/users`
- `GET /api/v1/users/{id}`
- `POST /api/v1/users`
- `PUT /api/v1/users/{id}`
- `DELETE /api/v1/users/{id}`

## Decisions

- List and filter share one paginated endpoint with `search`, `isActive`, `role`, `pageNumber`, and `pageSize` query parameters.
- Delete is a soft delete implemented by setting `IsActive` to `false`.
- Deactivation revokes all active refresh-token sessions and updates the Identity security stamp.
- An administrator cannot deactivate their own account through this endpoint.
- Newly created users receive only the system `User` role; role assignment remains a separately authorized operation.
- Changing an email marks it unconfirmed and revokes active refresh-token sessions.

## Acceptance criteria

- [x] Application layer exposes no ASP.NET Core Identity or EF Core types.
- [x] Queries support pagination and server-side filtering.
- [x] Create uses the configured Argon2id Identity password hasher.
- [x] Update detects duplicate emails.
- [x] Delete preserves the user record and revokes sessions.
- [x] Endpoints require the appropriate `users.*` permission.
- [x] Automated API tests pass.
- [x] Full solution build and test suite pass (19 tests passed).
