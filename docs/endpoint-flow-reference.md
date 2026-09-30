# Library Management API — Authentication, Authorization and Endpoint Flow Reference

> Source of truth: code at branch `develop`, commit `ae88860b3cc56e291a913140de3f55671ca97bc9`.
> This document describes runtime behavior, not the older README descriptions.

## 1. How to read this document

Every protected endpoint first passes through the common pipeline in section 2. Endpoint-specific sections then describe:

- input accepted by the controller;
- controller → application/identity service → repository/EF Core path;
- principal database tables read or written;
- successful HTTP response;
- alternative/error flows specific to that endpoint.

Common alternatives referenced below:

- `C1 — 400`: malformed JSON/form data, model binding failure, or explicit business input rejected as a bad request.
- `C2 — 401`: missing/invalid/expired JWT, invalid `sub`/`sid`, inactive account/employee, or inactive session family.
- `C3 — 403`: authenticated user lacks the required permission or violates branch/ownership rules.
- `C4 — 404`: requested resource does not exist or is intentionally hidden by ownership filtering.
- `C5 — 409`: duplicate/invalid state transition, protected resource, stale concurrency token, or database uniqueness conflict.
- `C6 — 422`: request is syntactically valid but fails application validation.
- `C7 — 429`: rate limiter rejects the request.
- `C8 — 500`: unhandled server error; response hides exception details when processed by `ApiExceptionHandler`.
- `C9 — 503`: configured external service, currently the embedding provider, is unavailable.

The global exception mapping is:

```text
RequestValidationException       -> 422 validation.failed
ResourceNotFoundException        -> 404 resource.not_found
ResourceConflictException        -> 409 resource.conflict
OptimisticConcurrencyException   -> 409 concurrency.conflict
UnauthorizedAccessException      -> 403 authorization.forbidden
ExternalServiceUnavailableException -> 503 external_service.unavailable
other exception                  -> 500 server.error
```

All Problem Details responses may carry `code` and `correlationId`.

## 2. Common HTTP request pipeline

```text
Client
  |
  | HTTP request
  v
ForwardedHeaders
  | trusts only configured reverse proxies
  v
ApiExceptionHandler
  | converts known exceptions to Problem Details
  v
CorrelationIdMiddleware
  | establishes request trace/correlation id
  v
RateLimiter
  | selected endpoints only
  v
JwtBearer Authentication
  | validate RSA signature, issuer, audience, lifetime, sub and sid
  | query current account/employee/session state
  v
Authorization policy
  | query current roles and permissions from PostgreSQL
  v
ASP.NET model binding + validation
  v
Controller
  | maps API contract to command/query
  v
Application or Identity service
  | validates business rules and coordinates transaction
  v
Repository / UserManager / EF Core DbContext
  | SQL against PostgreSQL
  v
Domain entity
  | enforces state transitions/invariants
  v
SaveChanges / commit
  | audit interceptor may append audit_logs
  v
Controller maps model to response DTO
  |
  +--> JSON 200/201
  +--> 204 No Content
  +--> file/stream response
  +--> Problem Details alternative flow
```

The main registration files are:

- `Backend/src/UTH.Library.Api/Program.cs`: middleware order and controller/SignalR mapping.
- `Backend/src/UTH.Library.Api/DependencyInjection.cs`: JWT authentication, permission policies, rate limits and Problem Details.
- `Backend/src/UTH.Library.Application/DependencyInjection.cs`: application services.
- `Backend/src/UTH.Library.Infrastructure/DependencyInjection.cs`: DbContext, repositories, Identity, JWT/RSA, SMTP and hosted services.
- `Backend/src/UTH.Library.Api/Infrastructure/ApiExceptionHandler.cs`: global exception-to-response conversion.

## 3. Authentication flow

### 3.1 Login

```text
LoginPage
  -> auth-api.login(email, password)
  -> clear access token held in browser memory
  -> navigator.locks("uth-session-cookie")
  -> POST /api/v1/auth/login
       X-Requested-With: XMLHttpRequest
       credentials: include
  -> AuthController rejects cross-site/non-XHR-shaped browser requests
  -> AuthService.LoginAsync
       UserManager.FindByEmailAsync
       SignInManager.CheckPasswordSignInAsync(lockoutOnFailure=true)
       Argon2PasswordHasher verifies password
       AuthorizationStateService verifies:
         users.IsActive
         optional email confirmation
         lockout has elapsed
         linked employees.Status == Active
       load active roles and effective permissions
       begin DB transaction
       PostgreSQL advisory transaction lock by UserId
       update users.LastLoginAtUtc
       create new refresh-token family
       add session.logged-in audit row
       create RS256 access token
       commit
  -> AuthController writes raw refresh token to uth_refresh HttpOnly cookie
  -> response 200 SessionResponse(accessToken, expiry, currentUser)
  -> frontend keeps access token only in JavaScript memory
```

Login alternatives:

- missing/incorrect credentials, unconfirmed email when required, inactive account, inactive/missing employee, or lockout → `401` with a deliberately generic message;
- more than 10 login requests per IP/minute → `429`;
- invalid browser-request headers/cross-site request → `403`;
- five failed password attempts invoke the Identity lockout configuration for 15 minutes.

### 3.2 Password hashing

```text
Algorithm: Argon2id v19
Memory: 65,536 KiB
Iterations: 3
Parallelism: 2
Salt: 16 random bytes
Output: 32 bytes
Stored form:
$argon2id$v=19$m=65536,t=3,p=2$<salt-base64>$<hash-base64>
```

Verification derives the candidate hash and uses constant-time comparison. Legacy ASP.NET Identity hashes are accepted and return `SuccessRehashNeeded` so they can migrate to Argon2id.

### 3.3 Access-token contents

```text
header: alg=RS256, kid=<Jwt.KeyId>
claims:
  sub         user id
  jti         unique access-token id
  email       login email
  iat         issued time
  sid         refresh-token family id
  role        repeated role claims
  permission  repeated permission claims
  iss/aud/nbf/exp standard validation values
```

The access token lives for 10 minutes and is never persisted by the frontend. A page reload therefore uses the refresh cookie to obtain a new access token.

### 3.4 Refresh-token rotation and session model

`refresh_token_sessions` stores `Id`, `UserId`, SHA-256 `TokenHash`, `FamilyId`, `ParentTokenId`, `ReplacedByTokenId`, creation/expiry/use/revocation timestamps, IP addresses, user agent, revocation reason and concurrency row version. The raw token is 32 cryptographically random bytes and only exists in the browser's `uth_refresh` cookie.

```text
POST /api/v1/auth/refresh
  -> read uth_refresh cookie
  -> SHA-256(raw token)
  -> find refresh_token_sessions row
  -> PostgreSQL advisory lock by UserId
  -> reload session under the lock
  -> if used/revoked:
       detect token reuse
       revoke every row in the FamilyId
       audit session.refresh-reuse-detected
       return 401
  -> if expired: return 401
  -> verify current account + employee status
  -> atomically mark old token:
       UsedAtUtc=now
       RevokedAtUtc=now
       RevocationReason=rotated
       ReplacedByTokenId=<new id>
  -> create replacement token:
       same FamilyId
       ParentTokenId=<old id>
       same absolute ExpiresAtUtc
  -> issue access token with sid=<FamilyId>
  -> replace cookie
  -> return 200 SessionResponse
```

Rotation does not extend the original 30-day absolute lifetime. Browser tabs serialize cookie rotation with `navigator.locks`; the server independently serializes login, refresh, logout and password changes with `pg_advisory_xact_lock`.

### 3.5 Authentication of every protected request

```text
Authorization: Bearer <JWT>
  -> validate RS256 signature with public key
  -> validate issuer, audience, exp/nbf and allowed algorithm
  -> parse sub and sid
  -> query current user and linked employee
  -> require account and employee active
  -> require at least one unexpired, unused, unrevoked refresh row
     for (UserId, FamilyId)
  -> establish authenticated ClaimsPrincipal
```

For SignalR only, `access_token` may be read from the query string when the path is `/api/v1/notifications/hub`.

### 3.6 Logout and forced invalidation

```text
POST /auth/logout or /auth/revoke
  -> locate cookie token
  -> revoke every row in that FamilyId
  -> reason=logout
  -> delete cookie

POST /auth/logout-all
  -> revoke every active refresh session for UserId
  -> reason=logout-all
  -> delete current cookie

POST /me/change-password
  -> verify current password and save new Argon2id hash
  -> revoke every active refresh session
  -> reason=password-changed
  -> delete cookie

Administrator lock/reset or employee deactivation
  -> revoke every active session with the corresponding reason
```

Because every access token carries `sid` and every request checks that family in the database, revoking a family invalidates its existing access tokens without a `jti` blacklist.

### 3.7 RSA PEM lifecycle

```text
API startup
  -> bind JwtOptions
  -> resolve PEM paths relative to ContentRootPath
  -> import private RSA PEM
  -> import public RSA PEM
  -> require each key >= 2048 bits
  -> compare public modulus and exponent in constant time
  -> fail startup if files are absent, malformed, weak or mismatched
  -> expose SigningKey(private) and ValidationKey(public)

Issue token:
  header + payload --RS256/private key--> signature

Validate token:
  header + payload + signature --public key--> valid or 401
```

Keys are loaded as singletons and validated by `JwtKeyValidationHostedService`. There is no JWKS or overlapping multi-key rotation; replacing the pair requires an API restart and immediately invalidates tokens signed by the previous key.

## 4. Authorization flow

```text
users
  -> AspNetUserRoles
  -> active roles
  -> role_permissions
  -> permissions
```

At startup, every constant in `Permissions.All` becomes an ASP.NET policy. Controllers use attributes such as `[Authorize(Policy = Permissions.BooksCreate)]`.

```text
PermissionAuthorizationHandler
  -> read UserId from sub
  -> AuthorizationStateService.GetAsync(UserId)
  -> query active roles and current permissions
  -> require CanAuthenticate
  -> require effective permission contains policy name
  -> success, otherwise 403
```

The backend policy uses current database permissions, not JWT permission claims. Role/permission changes therefore affect the next protected request without waiting for token expiry.

Frontend enforcement is complementary:

- `app/navigation.ts` hides menu entries;
- `ProtectedRoute.tsx` redirects unauthenticated users and renders a forbidden page when route permissions are missing;
- `PermissionBoundary.tsx` hides buttons/actions;
- `AuthProvider.tsx` refreshes authorization every 30 seconds, on focus/visibility, after a 403, and across tabs with `BroadcastChannel`;
- role/permission changes clear React Query cache.

Frontend checks are UX only; backend policies are authoritative.

## 5. Identity and account-management endpoints

Primary tables: `users`, `employees`, `roles`, `permissions`, `AspNetUserRoles`, `role_permissions`, `refresh_token_sessions`, `audit_logs`.

### 5.1 Authentication and current profile

| Endpoint | Request → database → response | Alternative flow |
|---|---|---|
| `POST /api/v1/auth/login` | Validate browser headers and credentials → Identity/Argon2id → current user/employee/roles/permissions → insert refresh session and audit → sign JWT → `200 SessionResponse` plus refresh cookie. | Generic `401`; `403` browser guard; `429`; `C8`. |
| `POST /api/v1/auth/refresh` | Hash cookie → lock user session → rotate refresh row → issue JWT/cookie → `200 SessionResponse`. | Missing/unknown/expired cookie `401`; reuse revokes family and returns `401`; `429`. |
| `POST /api/v1/auth/logout`, `POST /api/v1/auth/revoke` | Revoke current family, audit, delete cookie → `204`. Unknown cookie is still idempotent `204`. | Browser guard `403`; `C8`. |
| `POST /api/v1/auth/logout-all` | Require JWT → revoke all user sessions, audit, delete cookie → `204`. | `C2`; browser guard `403`. |
| `GET /api/v1/auth/me`, `GET /api/v1/auth/current-session` | Load current authorization + user + employee + branch → `200 CurrentProfileResponse`. | Unavailable profile `401`; `C2`. |
| `GET /api/v1/me` | Same current-profile query → `200 CurrentProfileResponse`. | Invalid `sub`/unavailable account `401`. |
| `PATCH /api/v1/me/profile` | Validate DOB and concurrency token → update employee personal fields and user display name → audit → `200 CurrentProfileResponse`. | Future DOB/invalid values `422`; stale row version `409`; missing profile `404`; `C2`. |
| `POST /api/v1/me/change-password` | Verify current password → update hash → revoke all sessions → audit → delete cookie → `204`. | Wrong/current policy-invalid password `422`; `C2`; DB conflict `C5`. |

### 5.2 Access accounts

| Endpoint | Request → database → response | Alternative flow |
|---|---|---|
| `GET /api/v1/access-accounts` | `users` filtered/paged, join employee and roles → `200 AccessAccountPageResponse`. | `C2/C3(users.read)`; validation/paging errors `C1/C6`. |
| `GET /api/v1/access-accounts/eligible-employees` | Search active/unlinked employees → `200` collection. | `C2/C3(users.create)`. |
| `GET /api/v1/access-accounts/{id}` | User + employee + roles → `200`. | `404`; `C2/C3(users.read)`. |
| `POST /api/v1/access-accounts` | Validate employee not linked, email unique and roles active → create Identity user/password → assign roles → link employee → audit → `201`. | Missing employee/role `404/422`; duplicate account/email `409`; protected/validation `409/422`. |
| `PATCH /api/v1/access-accounts/{id}/status` | Change `users.IsActive`; when disabling revoke all sessions → audit → `200`. | Self/protected account or invalid transition `409`; missing `404`; invalid data `422`. |
| `POST /api/v1/access-accounts/{id}/reset-password` | Generate reset token → reset to supplied temporary password → revoke all sessions → audit → `204`. | Missing `404`; protected/conflict `409`; password validation `422`. |
| `GET /api/v1/access-accounts/{id}/sessions` | Read refresh rows and compute `IsActive` → `200` collection. | Account `404`; `C2/C3(users.read)`. |
| `DELETE /api/v1/access-accounts/{id}/sessions/{sessionId}` | Find session owned by account → revoke entire family → audit → `204`. | Account/session `404`; protected/conflict `409`; `C3(users.update)`. |
| `PUT /api/v1/access-accounts/{id}/roles` | Verify account → replace `AspNetUserRoles` assignments with active requested roles → audit → `204`. | User/role `404`; protected/conflict `409`; invalid role set `422`; `C3(roles.assign)`. |

### 5.3 Legacy/general users API

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/users` | Filter/page `users`, load roles → `200 UserPageResponse`. | `C2/C3(users.read)`. |
| `GET /api/v1/users/{id}` | User + roles → `200 UserResponse`. | `404`; `C2/C3`. |
| `POST /api/v1/users` | Validate unique email/optional employee → create Identity user and audit → `201`. | validation `400`; duplicate/link conflict `409`; `C2/C3(users.create)`. |
| `PUT /api/v1/users/{id}` | Update email/display name and normalized identity fields → audit → `200`. | `404`; self/protected/duplicate conflict `409`; validation `400`. |
| `DELETE /api/v1/users/{id}` | Soft deactivate user, revoke sessions, audit → `204`. | `404`; self-deactivation/protected/conflict `409`; `C3(users.deactivate)`. |

### 5.4 Roles and permissions

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/roles` | Page/search roles and join permission assignments → `200 RolePageResponse`. | `C2/C3(roles.read)`. |
| `GET /api/v1/roles/{id}` | Role + permissions → `200 RoleResponse`. | `404`; `C2/C3`. |
| `POST /api/v1/roles` | Normalize/validate unique role name → insert role → `201`. | duplicate/invalid `400/409`; `C3(roles.create)`. |
| `PUT /api/v1/roles/{id}` | Update name/description/status → `200`. | `404`; system/protected or duplicate `409`; validation `400`. |
| `DELETE /api/v1/roles/{id}` | Ensure non-system/unassigned constraints → delete role and assignments → `204`. | `404`; protected/in-use `409`; `C3(roles.delete)`. |
| `PUT /api/v1/roles/{id}/permissions` | Validate all permission IDs → replace `role_permissions` in transaction → `200 RoleResponse`. | role/permission `404`; protected/conflict `409`; validation `400`. |
| `PUT /api/v1/users/{userId}/roles` | Validate user and active roles → diff/remove/add `AspNetUserRoles` → audit → `204`. | `404`; protected/conflict `409`; validation `400`. |
| `GET /api/v1/permissions` | Search/page permission catalog → `200 PermissionPageResponse`. | `C2/C3(permissions.read)`. |
| `GET /api/v1/permissions/modules` | Distinct modules → `200 string[]`. | `C2/C3`. |
| `GET /api/v1/permissions/{id}` | Read permission → `200`. | `404`; `C2/C3`. |
| `POST /api/v1/permissions` | Normalize/validate unique name/module → insert → `201`. | duplicate/validation `400/409`; `C3(permissions.create)`. |
| `PUT /api/v1/permissions/{id}` | Update non-system permission → `200`. | `404`; system/protected/duplicate `409`; validation `400`. |
| `DELETE /api/v1/permissions/{id}` | Ensure permission is not system/protected/in use → delete → `204`. | `404/409`; `C3(permissions.delete)`. |

### 5.5 Employees

Primary tables: `employees`, `branches`, optionally `users`, `refresh_token_sessions`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/employees` | Filter/page employees and branch → `200 EmployeePageResponse`. | `C2/C3(employees.read)`. |
| `GET /api/v1/employees/branches` | Read active branches for picker → `200`. | `C2/C3`. |
| `GET /api/v1/employees/{id}` | Employee + branch + linked account summary → `200`. | `404`; `C2/C3`. |
| `POST /api/v1/employees` | Validate code/email/branch/date fields and uniqueness → create employee → audit → `201`. | duplicate `409`; invalid branch/data `400/422`; `C3(employees.create)`. |
| `PUT /api/v1/employees/{id}` | Check concurrency token → validate and update employee → audit → `200`. | `404`; stale token/duplicate `409`; invalid input `400/422`. |
| `PATCH /api/v1/employees/{id}/status` | Change employment status; deactivation also disables linked authentication and revokes sessions → audit → `200`. | `404`; self/protected/invalid transition `409`; `C3(employees.update)`. |
| `DELETE /api/v1/employees/{id}` | Delete only when lifecycle constraints allow; linked account/dependencies are checked → audit → `204`. | `404`; linked/in-use/protected `409`; `C3(employees.delete)`. |

## 6. Catalog, copy, location and procurement endpoints

### 6.1 Books/catalog

Primary tables: `books`, `authors`, `publishers`, `categories`, `book_authors`, `book_categories`, `book_copies`, `book_embeddings`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/books` | Validate filters/page/sort → repository projects normalized catalog and copy counts → `200 BookPageResponse`. | `C2/C3(books.read)`; invalid query `422`. |
| `GET /api/v1/books/{id}` | Load book with references/counts → `200 BookResponse`. | `404`; `C2/C3`. |
| `GET /api/v1/books/catalog-references` | Select authors/publishers/categories according to reference type/search → `200`. | invalid reference type `422`; `C2/C3`. |
| `POST /api/v1/books` | Validate ISBN and metadata → resolve/create normalized references → enforce ISBN uniqueness → insert book/junction rows → audit → `201`. | duplicate ISBN `409`; invalid metadata `422`; missing reference `404`. |
| `PUT /api/v1/books/{id}` | Check concurrency token → validate/update catalog and junction rows → invalidate/update semantic embedding state → audit → `200`. | `404`; duplicate/stale token `409`; validation `422`. |
| `DELETE /api/v1/books/{id}` | Soft-deactivate catalog record when dependencies permit → audit → `204`. | `404`; active copies/loans/dependencies `409`. |
| `POST /api/v1/books/bulk-delete` | Iterate requested IDs through delete rules and collect per-row success/errors → `200 BulkResponse`. | empty/invalid set `422`; individual rows can fail without aborting whole result. |
| `GET /api/v1/books/export` | Query matching catalog → emit CSV/file response. | invalid filters `422`; `C2/C3`. |
| `POST /api/v1/books/import/preview` | Parse uploaded CSV, normalize ISBN/references, validate each row without saving → `200 BookImportPreviewResponse`. | missing/invalid/oversized file or malformed rows `400/422`. |
| `POST /api/v1/books/import/confirm` | Revalidate accepted rows → transactional create/update catalog records → return import result. | data changed/duplicates `409`; invalid rows `422`. |
| `POST /api/v1/books/semantic-search` | Validate query/topK → OpenAI embedding API → pgvector cosine search with category/availability filters → `200 SemanticBookSearchResponse`. | invalid query `422`; missing API key/provider failure `503`; rate limit `429`. |
| `POST /api/v1/books/semantic-embeddings/backfill` | Select books missing embeddings → build searchable text → request embeddings → upsert `book_embeddings` in transaction → `200 EmbeddingBackfillResponse`. | batch outside 1..100 `422`; provider `503`; `C3(books.update)`. |

### 6.2 Copies

Primary tables: `book_copies`, `books`, `shelves`, `areas`, `branches`, `stock_receipt_items`, `borrowings`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/copies` | Filter/page copies with book and location projections → `200` page. | invalid query `422`; `C2/C3(copies.read)`. |
| `GET /api/v1/copies/{id}` | Copy + book + shelf hierarchy → `200`. | `404`; `C2/C3`. |
| `GET /api/v1/copies/barcode/{barcode}` | Normalize barcode and load copy → `200`. | invalid/missing barcode `404/422`. |
| `POST /api/v1/copies` | Validate book, active shelf, unique normalized barcode and condition → insert copy → audit → `201`. | references `404`; duplicate/invalid state `409`; validation `422`. |
| `PATCH /api/v1/copies/{id}/status` | Check domain transition matrix → update status/concurrency → audit → `200`. | `404`; borrowed/invalid transition/stale token `409`. |
| `PATCH /api/v1/copies/{id}/relocate` | Require copy movable and destination shelf active → update ShelfId → audit → `200`. | copy/shelf `404`; borrowed/withdrawn/stale `409`. |
| `PATCH /api/v1/copies/{id}/condition` | Validate condition → update copy → audit → `200`. | `404`; invalid/stale `409/422`. |
| `PATCH /api/v1/copies/{id}/withdraw` | Require no active borrowing → set withdrawn status → audit → `200`. | `404`; active borrowing/already withdrawn/stale `409`. |
| `POST /api/v1/copies/bulk/{operation}` | Resolve operation and apply it per ID using update/withdraw permission rules → `200` per-item result list. | unknown operation `400`; operation-specific permission `403`; per-item failures returned in result. |
| `POST /api/v1/copies/import/preview` | Resolve each barcode/book/shelf row without saving → `200` preview rows. | malformed/duplicate references become row errors; request validation `422`. |
| `POST /api/v1/copies/import/confirm` | Re-resolve rows → transactional insert copies → `200` created models. | any unresolved row/duplicate/current-state conflict `409/422`. |
| `GET /api/v1/copies/export` | Query matching copies → CSV response. | invalid filters `422`; `C2/C3`. |

### 6.3 Locations

Primary tables: `branches`, `areas`, `shelves`, plus dependency counts from employees/copies/receipts/audits.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/locations` | Load branch→area→shelf tree, optionally inactive nodes → `200`. | `C2/C3(locations.read)`. |
| `GET /api/v1/locations/active-shelves` | Query active branch/area/shelf combinations → `200` picker rows. | `C2/C3`. |
| `GET /api/v1/locations/{type}/{id}/impact` | Validate type and count dependent records → `200 LocationImpactResponse`. | invalid type `400`; missing node `404`. |
| `POST /api/v1/locations/branches` | Validate unique code/name → insert branch → audit → `201`. | duplicate/invalid `409/422`. |
| `PUT /api/v1/locations/branches/{id}` | Check concurrency → update branch → audit → `200`. | `404`; duplicate/stale `409`. |
| `POST /api/v1/locations/areas` | Require active parent branch and unique scoped code/name → insert → `201`. | parent `404/409`; duplicate `409`; validation `422`. |
| `PUT /api/v1/locations/areas/{id}` | Check concurrency/parent rules → update → `200`. | `404/409/422`. |
| `POST /api/v1/locations/shelves` | Require active area hierarchy and unique code → insert shelf → `201`. | parent `404/409`; duplicate `409`; validation `422`. |
| `PUT /api/v1/locations/shelves/{id}` | Check capacity/code/concurrency → update shelf → `200`. | `404`; capacity below current usage or stale/duplicate `409`. |
| `POST .../branches/{id}/activate`, `POST .../areas/{id}/activate`, `POST .../shelves/{id}/activate` | Validate parent hierarchy and concurrency → activate → audit → `200`. | `404`; inactive parent/stale token `409`. |
| `DELETE .../branches/{id}`, `DELETE .../areas/{id}`, `DELETE .../shelves/{id}` | Impact check → soft deactivate node, with hierarchy rules → `200`. | `404`; active dependencies/stale/protected main branch `409`. |
| `DELETE .../{type}/{id}/permanent` | Require already-safe/no dependencies → physically delete node → audit → `204`. | `404`; any dependency/protected node `409`. |

### 6.4 Suppliers

Primary tables: `suppliers`, `stock_receipts`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/suppliers` | Filter/page suppliers → `200`. | invalid query `422`; `C3(suppliers.read)`. |
| `GET /api/v1/suppliers/active` | Read active suppliers for picker → `200`. | `C2/C3`. |
| `GET /api/v1/suppliers/{id}` | Read supplier → `200`. | `404`. |
| `POST /api/v1/suppliers` | Validate unique code/name/contact data → insert → audit → `201`. | duplicate `409`; validation `422`. |
| `PUT /api/v1/suppliers/{id}` | Check concurrency → update → `200`. | `404`; duplicate/stale `409`; validation `422`. |
| `POST /api/v1/suppliers/{id}/activate` | Activate supplier → audit → `200`. | `404`; stale token `409`. |
| `DELETE /api/v1/suppliers/{id}` | Soft deactivate supplier → audit → `200`. Existing receipts remain. | `404`; stale/current-state conflict `409`. |

### 6.5 Stock receipts

Primary tables: `stock_receipts`, `stock_receipt_items`, `suppliers`, `branches`, `books`, `book_copies`, `discrepancy_reports`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/stock-receipts` | Filter/page receipts with supplier/branch totals → `200`. | invalid filters `422`; `C3(stock-receipts.read)`. |
| `GET /api/v1/stock-receipts/{id}` | Load receipt and item detail → `200`. | `404`. |
| `POST /api/v1/stock-receipts` | Validate active supplier/branch, receipt number and book items → create draft/items → audit → `201`. | reference `404`; duplicate/invalid quantities `409/422`. |
| `PUT /api/v1/stock-receipts/{id}` | Require editable draft and matching concurrency token → replace/update items → audit → `200`. | `404`; confirmed/cancelled/stale `409`; validation `422`. |
| `GET /api/v1/stock-receipts/{id}/confirmation` | Load expected items, existing created copies and discrepancies → `200 ConfirmStockReceiptResult`. | `404`; `C3`. |
| `POST /api/v1/stock-receipts/{id}/confirm` | Lock/validate draft and token → verify actual quantities/barcodes/shelves → create copies → create discrepancy reports → mark confirmed/actor → commit → `200`. | duplicate barcode/reference `409`; invalid quantities/state/token `409/422`; `404`. |

### 6.6 Inventory audits

Primary tables: `inventory_audits`, `inventory_audit_items`, `book_copies`, location tables, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/inventory-audits/locations` | Read valid branch/area/shelf choices → `200`. | `C3(inventory-audits.read)`. |
| `GET /api/v1/inventory-audits` | Filter/page audit sessions → `200`. | invalid query `422`; `C3`. |
| `GET /api/v1/inventory-audits/{id}` | Load audit and scanned/expected items → `200`. | `404`. |
| `POST /api/v1/inventory-audits` | Require active branch + area + shelf and current actor → immediately create an `InProgress` audit, snapshot all expected copies into audit items, add audit event → `201`. | invalid/inactive scope `422`; missing authenticated actor `403`. |
| `POST /api/v1/inventory-audits/{id}/start` | Compatibility/idempotency endpoint: read the audit only; return it when already `InProgress` → `200`. It performs no start mutation because creation already starts the audit. | missing `404`; any status other than `InProgress` → empty `409`. |
| `POST /api/v1/inventory-audits/{id}/scan` | Normalize barcode → locate copy → validate audit scope and state → upsert scan/result → `200`. | copy/audit `404`; completed/out-of-scope/duplicate conflict `409`; validation `422`. |
| `GET /api/v1/inventory-audits/{id}/reconcile` | Compare expected and scanned copies, calculate missing/misplaced/unexpected items → `200`. | `404`; invalid lifecycle state `409`. |
| `POST /api/v1/inventory-audits/{id}/complete` | Reconcile → require acknowledgement when discrepancies exist → mark completed → audit → `200`. | `404`; unacknowledged discrepancies/stale state `409`. |
| `POST /api/v1/inventory-audits/{id}/apply` | Validate completed audit and 1..100 unique discrepancy corrections → check per-copy concurrency → relocate/change copy state/condition as approved → transaction + audit → `200 {appliedCount}`. | `404`; stale copy or invalid correction `409/422`. |
| `GET /api/v1/inventory-audits/{id}/export` | Load reconciled result → generate CSV → file response. | `404`; `C3(inventory-audits.export)`. |

## 7. Circulation, reservations, violations and member finance

### 7.1 Borrowings, checkout, return and renewal

Primary tables: `borrowings`, `renewals`, `members`, `membership_cards`, `member_restrictions`, `books`, `book_copies`, `circulation_policies`, `violations`, `fine_payments`, `fine_adjustments`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/borrowings` | Filter/page loans and book/member summaries → `200 BorrowingPageResponse`. | invalid filters `422`; `C3(borrowings.read)`. |
| `GET /api/v1/borrowings/{id}` | Loan + copy/book/member + renewals/return status → `200 BorrowingDetailResponse`. | `404`. |
| `POST /api/v1/borrowings` | Legacy create path: resolve active circulation policy → validate borrower/book rules → insert borrowing with policy snapshot → `201`. | duplicate active loan/policy restrictions `409`; missing reference `404`; validation `422`. |
| `POST /api/v1/borrowings/{id}/return` | Legacy return path: ensure open loan → mark returned and copy available → transaction/audit → `200`. | `404`; already returned/stale state `409`. |
| `GET /api/v1/borrowings/checkout/lookup-member` | Normalize card/code → member/card/restriction/open-loan/fine checks → return eligibility and limits → `200`. | member/card `404`; invalid search `400/422`. |
| `GET /api/v1/borrowings/checkout/lookup-copy` | Normalize barcode → copy/book/location/availability lookup → `200`. | copy `404`; unavailable status represented in response or conflict during checkout. |
| `POST /api/v1/borrowings/checkout` | Resolve member/card/copy/employee and applicable policy → enforce active card, restrictions, overdue block, loan limit, copy availability → transaction: copy Borrowed + borrowing/policy snapshot + audit → `201`. | missing entity `404`; invalid command `400/422`; blocked member/limit/duplicate copy/concurrency `409`. |
| `GET /api/v1/borrowings/return/lookup-copy` | Barcode → copy → active borrowing → policy/fine preview → `200 BookCopyReturnLookupResponse`. | no copy/open loan `404`. |
| `POST /api/v1/borrowings/return/confirm` | Load active loan/copy/member/policy → calculate overdue/lost/damaged outcome → mark returned → update copy condition/status → optionally create violation/fine → transaction/audit → `200 ReturnExecutionResponse`. | missing `404`; invalid return inputs `400/422`; already returned/copy mismatch/stale token `409`. |
| `GET /api/v1/borrowings/{id}/renewal-preview` | Load open loan/member/reservations/policy → calculate eligibility and new due date → `200 RenewalPreviewResponse`. | `404`; ineligible reason is returned in preview. |
| `POST /api/v1/borrowings/{id}/renew` | Re-evaluate eligibility → enforce max renewals, not overdue, no blocking reservation/restriction → update due date/count + insert renewal policy snapshot → audit → `200`. | explicit eligibility denial `403`; stale/already returned/max count `409`; `404`. |

### 7.2 Reservations

Primary tables: `reservations`, `books`, `members`, `membership_cards`, `member_restrictions`, `book_copies`, `circulation_policies`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/reservations` | Filter/page reservations and computed lifecycle status → `200 ReservationPageResponse`. | invalid query `422`; `C3(reservations.read)`. |
| `GET /api/v1/reservations/{id}` | Reservation + book/member/policy detail → `200 ReservationDetailResponse`. | `404`. |
| `POST /api/v1/reservations` | Resolve member/book/policy → validate card/restrictions/no duplicate open reservation → calculate hold expiry and save policy snapshot → `201`. | missing `404`; duplicate/ineligible `409`; validation `422`. |
| `POST /api/v1/reservations/{id}/cancel` | Require open reservation and concurrency token/reason → mark cancelled → audit → `200`. | `404`; fulfilled/already-cancelled/stale `409`. |
| `POST /api/v1/reservations/{id}/fulfill` | Require open/unexpired reservation and available matching copy → mark fulfilled; optionally coordinate checkout state according to command → audit → `200`. | `404`; expired/no available copy/already closed/stale `409`. |

### 7.3 Violations

Primary tables: `violations`, `borrowings`, `members`, `books`, `circulation_policies`, `fine_payments`, `fine_adjustments`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/violations` | Filter/page violations with outstanding balance → `200 ViolationPageResponse`. | invalid query `422`; `C3(violations.read)`. |
| `GET /api/v1/violations/{id}` | Violation + policy snapshot + payments/adjustments → `200 ViolationDetailResponse`. | `404`. |
| `POST /api/v1/violations/preview` | Resolve applicable policy; calculate overdue/fixed/lost/damaged fine and cap without saving → `200 FinePreviewResponse`. | missing references `404`; invalid type/input `422`. |
| `POST /api/v1/violations` | Check idempotency by borrowing/copy/type; an existing match is returned as success. Otherwise validate member/book, calculate or accept fine, insert policy/calculation snapshot and audit → controller returns `201`. | missing member/book `404`; invalid domain data `400`; persistence conflict `409`. |
| `POST /api/v1/violations/{id}/pay` | Legacy shortcut: validate open balance → record/resolve payment state → `200 ViolationResponse`. | `404`; already resolved/invalid balance `409/422`. |
| `POST /api/v1/violations/{id}/waive` | Legacy shortcut: validate open violation → waive/resolve → audit → `200`. | `404`; already resolved `409`. |

### 7.4 Fine payments

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/payments` | Filter/page `fine_payments` joined to violations/members → `200 FinePaymentPageResponse`. | invalid query `422`; `C3(violations.read)`. |
| `GET /api/v1/payments/{id}` | Payment receipt detail → `200 FinePaymentReceiptResponse`. | `404`. |
| `POST /api/v1/payments/preview` | Load violation, sum payments/adjustments and calculate remaining payable amount → `200 FinePaymentPreviewResponse`. | violation `404`. |
| `POST /api/v1/payments` | Validate member/violation/amount/method/idempotency and current balance → insert payment, update resolution when fully paid, audit → `201`. | references `404`; overpayment/duplicate/stale balance `409`; invalid amount `400/422`. |

### 7.5 Members, cards, restrictions and finance

Primary tables: `members`, `membership_cards`, `member_restrictions`, `borrowings`, `reservations`, `violations`, `fine_payments`, `fine_adjustments`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/members` | Filter/page members; PII masking depends on `members.update` claim → `200 MemberPageResponse`. | `C2/C3(members.read)`. |
| `GET /api/v1/members/{id}` | Member + current card/restrictions/loan/reservation/fine summary → `200`. | `404`. |
| `GET /api/v1/members/{id}/history` | Validate history category → query paged activity → `200 MemberHistoryPageResponse`. | member/category `404/400`. |
| `POST /api/v1/members` | Validate unique member code/email and limits → insert → audit → `201`. | duplicate `409`; validation `400`. |
| `PUT /api/v1/members/{id}` | Check concurrency token → update profile/group/status/limits → audit → `200`. | `404`; duplicate/stale `409`; validation `400`. |
| `POST /api/v1/members/{id}/card` | Parse wrapped/unwrapped payload → validate dates/card number/token → issue card → audit → `200`. | malformed payload `400`; member `404`; duplicate/active-card/stale `409`. |
| `POST /api/v1/members/{id}/card/renew` | Validate expiry and concurrency → renew current card → `200`. | no card/member `404`; invalid/revoked/stale `409/400`. |
| `PATCH /api/v1/members/{id}/card/status` | Validate transition → update card status → `200`. | `404`; invalid transition/stale `409`. |
| `POST /api/v1/members/{id}/restrictions` | Validate period/type/reason/token → insert restriction → `200`. | `404`; overlapping/invalid/stale `409/400`. |
| `POST /api/v1/members/{id}/restrictions/{restrictionId}/remove` | Find owned active restriction → mark removed/reason → `200`. | `404`; already removed/stale `409`. |
| `POST /api/v1/members/{id}/violations/{violationId}/payments` | Validate ownership/current balance → insert payment → recompute fine state → `200 MemberResponse`. | `404`; overpayment/duplicate `409`; invalid amount `400`. |
| `POST /api/v1/members/{id}/violations/{violationId}/adjustments` | Validate ownership/delta/reason → insert adjustment → recompute balance → `200`. | `404`; invalid negative result/state `409/400`. |

## 8. Policy, configuration, audit and dashboard

### 8.1 Circulation policies

Primary tables: `circulation_policies`; borrowing/reservation/violation records keep immutable applied-policy snapshots.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/circulation-policies` | Filter/page policies and scope/version state → `200`. | invalid filters `422`; `C3(read)`. |
| `GET /api/v1/circulation-policies/{id}` | Read policy → `200`. | `404`. |
| `POST /api/v1/circulation-policies` | Validate amounts/dates/scope and non-overlap → insert version → audit → `201`. | invalid `400/422`; overlapping/duplicate `409`. |
| `PUT /api/v1/circulation-policies/{id}` | Check concurrency and editable state → update → audit → `200`. | `404`; active/used/stale/overlap `409`; validation `400`. |
| `POST /api/v1/circulation-policies/{id}/versions` | Load source → create next immutable version with requested changes → `201`. | `404`; validation/overlap `400/409`. |
| `POST /api/v1/circulation-policies/{id}/activate` | Check effective dates and overlapping active policy → activate/deactivate conflicts as service defines → `200`. | `404`; overlap/stale `409`. |
| `POST /api/v1/circulation-policies/{id}/deactivate` | Mark inactive → `200`. | `404`; stale/current-state `409`. |
| `POST /api/v1/circulation-policies/preview` | Resolve best matching policy by member group/document type/branch/date and return computed values without saving → `200`. | no applicable policy/invalid input `400/404`. |

### 8.2 System settings and configuration packages

Primary tables: `system_settings`, `configuration_packages`, `audit_logs`; secret setting values are protected before persistence/output.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/settings` | Read settings; redact secret values but return `HasValue` → `200 SystemSettingResponse[]`. | `C2/C3(settings.read)`. |
| `PUT /api/v1/settings/{key}` | Validate type/scope/value and concurrency token → protect secret if needed → update/audit → `200`. | unknown key `404`; stale token `409`; invalid typed value `422`; `C3(settings.update)`. |
| `POST /api/v1/configuration/export` | Serialize exportable settings, checksum/schema metadata → insert package audit record as applicable → JSON file plus checksum/version headers. | `C2/C3(settings.read)`; `C8`. |
| `POST /api/v1/configuration/import/validate` | Enforce non-empty ≤1 MiB file → parse schema/checksum/expiry/types → compare to current settings → store signed/temporary confirmation state → `200 preview + confirmationToken`. | file `422`; malformed/version/checksum/expired package `422/409`. |
| `POST /api/v1/configuration/import/confirm` | Validate confirmation token and unchanged baseline → transactionally apply settings and insert package/audit → `200 counts/checksum`. | missing/expired/replayed token or changed state `409`; invalid data `422`. |

### 8.3 Audit logs

Primary table: `audit_logs`, with optional user/employee projection for actor name.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/audit-log` | Validate time range/IP → filter/page immutable audit records → `200 AuditLogPageResponse`. | invalid time/IP `400`; `C3(audit-logs.read)`. |
| `GET /api/v1/audit-log/{id}` | Read audit row → `200`. | `404`; `C3`. |
| `GET /api/v1/audit-log/entities/{entityType}/{entityId}` | Filter audit rows by entity and page → `200`. | invalid paging `422`; `C3`. |
| `GET /api/v1/audit-log/export` | Validate filters → query export set → CSV with UTF-8 BOM and spreadsheet-formula escaping → file response. | invalid time/IP `400`; `C3(audit-logs.export)`. |

### 8.4 Dashboard

Primary reads: books/copies, borrowings, members, reservations, violations/payments, inventory items, audit logs and branches.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/dashboard/summary` | Load profile/branch scope → normalize range/timezone → aggregate KPIs, operational alerts and recent activities → `200 DashboardSummaryResponse`. Non-admin staff are constrained to their branch. | invalid range/branch `422/404`; invalid principal `401`; `C8`. |
| `GET /api/v1/dashboard/branches` | Query active branches for dashboard filter → `200`. | `C2`; `C8`. |

## 9. Notifications, SMTP and realtime

### 9.1 Notification templates

Primary tables: `notification_templates`, `audit_logs`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/notification-templates/events` | Return in-memory supported event/variable definitions → `200`. | `C2`. |
| `GET /api/v1/notification-templates` | Query non-SMS templates → `200`. | `C2`. |
| `GET /api/v1/notification-templates/{id}` | Read template → `200`. | `404`; `C2`. |
| `POST /api/v1/notification-templates` | Manual admin/permission check → validate channel/code/templates/allowed variables → enforce unique code → insert/audit → `201`. | permission `403`; invalid `400`; duplicate `409`; `500`. |
| `PUT /api/v1/notification-templates/{id}` | Manual permission check → validate concurrency and variables → update/audit → `200`. | `403`; `404`; invalid `400`; stale/duplicate `409`; `500`. |
| `DELETE /api/v1/notification-templates/{id}` | Manual permission check → delete only when allowed by usage/system rules → audit → `204`. | `403`; `404`; in-use/protected `409/400`; `500`. |

### 9.2 Notification delivery and inbox

Primary tables: `notifications`, `notification_templates`, `employees`, `members`, identity role/permission tables, `audit_logs`. Email delivery additionally reads protected SMTP settings.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `POST /api/v1/notifications/preview` | Load template by code → validate supplied variables → render subject/body without persistence → `200 NotificationPreviewResult`. | template `404`; missing/unsupported variables `400`; current controller converts unexpected errors to `500`. |
| `POST /api/v1/notifications/send` | Require admin or `notifications.manage` → resolve recipient/destination → validate event, severity, deep-link allow-list and idempotency → render → insert notification/audit → in-app publish or email outbox/send path → `200 NotificationDto`. | `403`; template/recipient `404`; invalid/unsupported channel `400`; `500`. |
| `POST /api/v1/notifications/send-bulk` | Permission + branch constraint → resolve explicit/all-staff/role/permission recipients → deduplicate → send each with idempotency derivation → audit bulk operation → `200 BulkNotificationResult`. | cross-branch/permission `403`; invalid selection `400`; duplicate/idempotency conflict `409`; template `404`. |
| `POST /api/v1/notifications/{id}/retry` | Require manage permission → load failed email → reset/send through SMTP adapter → update status/attempt metadata → audit → `200`. | `403`; `404`; non-email/already-sent/invalid state `400`; SMTP failure remains represented in notification or `500` by controller. |
| `GET /api/v1/notifications/history` | Require read/manage permission → filter/page all operational notifications → `200 NotificationPageResult`. | `403`; invalid filters `422`. |
| `GET /api/v1/notifications/my` | Query only in-app notifications owned by current UserId with unread/severity/date filters → `200`. | `C2`; invalid filters `422`. |
| `GET /api/v1/notifications/my/{id}` | Ownership + in-app channel filter → `200`. | hidden/missing/not-owned `404`. |
| `GET /api/v1/notifications/unread-count` | Count current user's unread in-app notifications → `200 {count}`. | `C2`. |
| `PUT /api/v1/notifications/{id}/read` | Load notification → enforce recipient ownership and in-app channel → set `ReadAtUtc` → `204`. | not found `404`; ownership violation `403`. |
| `PUT /api/v1/notifications/read-all` | Bulk update current user's unread in-app notifications → `204`. | `C2`; `C8`. |
| `GET /api/v1/notifications/recipients` | Use current roles/permissions to search allowed staff/member recipient type → `200`. | unauthorized recipient scope `403`; invalid type `400`. |

### 9.3 SMTP

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/smtp` | Read typed SMTP settings from protected system settings; return redacted password/HasPassword → `200 SmtpSettingsView`. | `C2/C3(settings.read)`. |
| `POST /api/v1/smtp/test` | Load/decrypt settings → validate host/port/security/recipient → optionally connect only or send test message → `200 SmtpTestResult`. | failed validation/connection/send → `400` with result message; `C3(settings.update)`. |

### 9.4 Realtime hub

```text
SignalR connects to /api/v1/notifications/hub
  -> accessTokenFactory obtains/refreshed JWT
  -> JwtBearer reads access_token only for hub path
  -> normal RSA/account/session authentication
  -> NotificationHub reads sub
  -> joins group user:<UserId>
  -> NotificationService publishes NotificationReceived to that group
  -> frontend increments unread cache and invalidates notification lists
  -> automatic reconnect schedule: 0s, 2s, 5s, 10s, 30s
  -> polling/query refresh remains fallback
```

## 10. Reports and saved filters

### 10.1 Reports

Primary reads vary by report definition; persisted exports use `reports`. Access is constrained by permission and branch.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/reports/definitions` | Load current profile → filter in-memory report definitions by admin/effective permissions → `200`. | invalid principal `401`; unavailable profile yields empty/limited definitions. |
| `POST /api/v1/reports/preview` | Resolve definition and required permission → enforce user branch unless global → validate filters/sort/timezone → repository query/project/page → `200 ReportPreviewResult`. | definition `404`; permission/branch `403`; current controller maps other exceptions to `500`. |
| `POST /api/v1/reports/export` | Same authorization/query path without page limit → generate CSV/file bytes → persist report metadata/owner/expiry → `200 ReportExportResult`. | `404`; `403`; generation/storage error `500`. |
| `GET /api/v1/reports/{id}/download` | Load report metadata → enforce owner or admin → require unexpired artifact and available bytes → file response. | `404`; not owner `403`; expired `410`; missing file `404`. |

### 10.2 Saved filters

Primary table: `saved_filters`.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/saved-filters?scope=` | Query only current user's filters for scope → `200`. | invalid principal `401`; invalid scope `400/422`. |
| `POST /api/v1/saved-filters` | Validate name/scope/criteria/sort JSON → insert with owner → `201`. | validation `400`; duplicate/conflict `409`. |
| `PUT /api/v1/saved-filters/{id}` | Update only when `CreatedByUserId` matches current user → `204`. | missing or not-owned `404`; validation `400`. |
| `DELETE /api/v1/saved-filters/{id}` | Delete only owned filter → `204`. | missing or not-owned `404`. |

## 11. Public kiosk endpoints

These endpoints are `[AllowAnonymous]`. Rate limiting still applies to search. Data is read-only and the projection intentionally exposes public catalog fields only.

Primary reads: `books`, normalized catalog tables, `book_copies`, location tables, and `book_embeddings` for semantic search.

| Endpoint | Flow and success | Alternatives |
|---|---|---|
| `GET /api/v1/kiosk/books` | Validate search/filter/page → query active books and public availability counts → `200 KioskBookPageResponse`. | invalid query `400/422`; search rate >120/IP/min `429`. |
| `POST /api/v1/kiosk/books/semantic-search` | Validate query/topK → embedding provider → pgvector search → map public result fields → `200 KioskSemanticSearchResponse`. | validation `422`; provider `503`; >20/IP/min `429`. |
| `GET /api/v1/kiosk/books/{id}` | Load active public book detail, copies and public locations → `200 KioskBookDetailResponse`. | inactive/missing book `404`. |

## 12. Non-runtime Todo controller

`TodosController` contains `POST /api/todos`, `GET /api/todos/{id}` and `POST /api/todos/{id}/complete`, but the class is marked `[NonController]`. ASP.NET therefore does not discover or expose these routes. Its service/repository/table remain scaffold/test material, not part of the runtime endpoint surface.

## 13. Database transaction, audit and concurrency behavior

```text
Read endpoint
  -> AsNoTracking query where possible
  -> projection DTO
  -> no SaveChanges

Simple write endpoint
  -> load aggregate
  -> validate domain transition
  -> mutate entity
  -> SaveChangesAsync
  -> EF uniqueness/concurrency translated to 409 as applicable

Multi-aggregate write
  -> IUnitOfWork.ExecuteAsync / explicit transaction
  -> update all participating entities
  -> append audit rows
  -> one commit or rollback
```

Core records use GUID concurrency tokens. API commands commonly carry the token read by the client. A mismatch or EF `DbUpdateConcurrencyException` results in `409`, requiring the client to reload before retrying.

`AuditSaveChangesInterceptor` creates change history for configured EF changes. Services also append explicit audit events for identity/session and higher-level operations. Audit entries capture actor, action, entity, before/after JSON when available, timestamp, correlation id and IP.

## 14. Response shapes and client handling

```text
200 OK
  successful reads, updates, previews, actions returning a body

201 Created
  successful creates; usually Location/CreatedAtAction points to detail endpoint

204 No Content
  successful delete/revoke/mark-read/change-password operations

400 Bad Request
  controller-level input/business validation in older/manual controllers

401 Unauthorized
  authentication/session failure

403 Forbidden
  current identity lacks permission/scope/ownership

404 Not Found
  resource missing or ownership-filtered

409 Conflict
  concurrency, duplicate, protected resource, invalid lifecycle transition

410 Gone
  expired report artifact

422 Unprocessable Entity
  structured application validation

429 Too Many Requests
  login, refresh or kiosk rate limit

500 Internal Server Error
  unhandled failure

503 Service Unavailable
  embedding/external provider unavailable
```

Frontend API calls normally use `authenticatedFetch`:

```text
send Bearer token
  -> response != 401: return it
  -> response == 401: rotate refresh token once and retry once
  -> second 401: clear local session
  -> any 403: trigger authorization profile refresh
```


## Appendix A — Exact runtime route inventory

Total mapped routes (aliases counted separately): **196**.

### AccessAccounts

- `GET /api/v1/access-accounts` — UsersRead; action `Get`.
- `GET /api/v1/access-accounts/eligible-employees` — UsersCreate; action `GetEligibleEmployees`.
- `GET /api/v1/access-accounts/{id:guid}` — UsersRead; action `GetById`.
- `POST /api/v1/access-accounts` — UsersCreate; action `Create`.
- `PATCH /api/v1/access-accounts/{id:guid}/status` — UsersDeactivate; action `SetStatus`.
- `POST /api/v1/access-accounts/{id:guid}/reset-password` — UsersUpdate; action `ResetPassword`.
- `GET /api/v1/access-accounts/{id:guid}/sessions` — UsersRead; action `GetSessions`.
- `DELETE /api/v1/access-accounts/{id:guid}/sessions/{sessionId:guid}` — UsersUpdate; action `RevokeSession`.
- `PUT /api/v1/access-accounts/{id:guid}/roles` — RolesAssign; action `ReplaceRoles`.

### AuditLogs

- `GET /api/v1/audit-log` — AuditLogsRead; action `Get`.
- `GET /api/v1/audit-log/export` — AuditLogsExport; action `Export`.
- `GET /api/v1/audit-log/{id:guid}` — AuditLogsRead; action `GetById`.
- `GET /api/v1/audit-log/entities/{entityType}/{entityId:guid}` — AuditLogsRead; action `GetEntityHistory`.

### Auth

- `POST /api/v1/auth/login` — anonymous; action `Login`.
- `POST /api/v1/auth/refresh` — anonymous; action `Refresh`.
- `POST /api/v1/auth/logout` — anonymous; action `Logout`.
- `POST /api/v1/auth/revoke` — anonymous; action `Logout`.
- `POST /api/v1/auth/logout-all` — anonymous; action `LogoutAll`.
- `GET /api/v1/auth/me` — anonymous; action `Me`.
- `GET /api/v1/auth/current-session` — anonymous; action `Me`.

### Books

- `POST /api/v1/books/semantic-search` — BooksRead; action `SemanticSearch`.
- `POST /api/v1/books/semantic-embeddings/backfill` — BooksUpdate; action `BackfillEmbeddings`.
- `GET /api/v1/books/catalog-references` — BooksRead; action `GetCatalogReferences`.
- `GET /api/v1/books` — BooksRead; action `Get`.
- `GET /api/v1/books/export` — BooksRead; action `Export`.
- `POST /api/v1/books/import/preview` — BooksCreate; action `PreviewImport`.
- `POST /api/v1/books/import/confirm` — BooksCreate; action `ConfirmImport`.
- `POST /api/v1/books/bulk-delete` — BooksDelete; action `BulkDelete`.
- `GET /api/v1/books/{id:guid}` — BooksRead; action `GetById`.
- `POST /api/v1/books` — BooksCreate; action `Create`.
- `PUT /api/v1/books/{id:guid}` — BooksUpdate; action `Update`.
- `DELETE /api/v1/books/{id:guid}` — BooksDelete; action `Delete`.

### Borrowings

- `GET /api/v1/borrowings` — BorrowingsRead; action `Get`.
- `POST /api/v1/borrowings` — BorrowingsCreate; action `Create`.
- `POST /api/v1/borrowings/{id:guid}/return` — BorrowingsReturn; action `Return`.
- `GET /api/v1/borrowings/{id:guid}` — BorrowingsRead; action `GetById`.
- `GET /api/v1/borrowings/{id:guid}/renewal-preview` — BorrowingsRead; action `GetRenewalPreview`.
- `POST /api/v1/borrowings/{id:guid}/renew` — BorrowingsRenew; action `Renew`.
- `GET /api/v1/borrowings/checkout/lookup-member` — BorrowingsCreate; action `LookupMember`.
- `GET /api/v1/borrowings/checkout/lookup-copy` — BorrowingsCreate; action `LookupBookCopy`.
- `POST /api/v1/borrowings/checkout` — BorrowingsCreate; action `Checkout`.
- `GET /api/v1/borrowings/return/lookup-copy` — BorrowingsReturn; action `LookupCopyForReturn`.
- `POST /api/v1/borrowings/return/confirm` — BorrowingsReturn; action `ConfirmReturn`.

### CirculationPolicies

- `GET /api/v1/circulation-policies` — CirculationPoliciesRead; action `Get`.
- `GET /api/v1/circulation-policies/{id:guid}` — CirculationPoliciesRead; action `GetById`.
- `POST /api/v1/circulation-policies` — CirculationPoliciesManage; action `Create`.
- `PUT /api/v1/circulation-policies/{id:guid}` — CirculationPoliciesManage; action `Update`.
- `POST /api/v1/circulation-policies/{id:guid}/versions` — CirculationPoliciesManage; action `CreateVersion`.
- `POST /api/v1/circulation-policies/{id:guid}/activate` — CirculationPoliciesManage; action `Activate`.
- `POST /api/v1/circulation-policies/{id:guid}/deactivate` — CirculationPoliciesManage; action `Deactivate`.
- `POST /api/v1/circulation-policies/preview` — CirculationPoliciesRead; action `Preview`.

### Configuration

- `POST /api/v1/configuration/export` — SettingsRead; action `Export`.
- `POST /api/v1/configuration/import/validate` — SettingsUpdate; action `ValidateImport`.
- `POST /api/v1/configuration/import/confirm` — SettingsUpdate; action `ConfirmImport`.

### Copies

- `GET /api/v1/copies` — CopiesRead; action `Get`.
- `GET /api/v1/copies/{id:guid}` — CopiesRead; action `GetById`.
- `GET /api/v1/copies/barcode/{barcode}` — CopiesRead; action `GetByBarcode`.
- `POST /api/v1/copies` — CopiesCreate; action `Create`.
- `PATCH /api/v1/copies/{id:guid}/status` — CopiesUpdate; action `ChangeStatus`.
- `PATCH /api/v1/copies/{id:guid}/relocate` — CopiesUpdate; action `Relocate`.
- `POST /api/v1/copies/bulk/{operation}` — authenticated; action `Bulk`.
- `PATCH /api/v1/copies/{id:guid}/condition` — CopiesUpdate; action `ChangeCondition`.
- `PATCH /api/v1/copies/{id:guid}/withdraw` — CopiesWithdraw; action `Withdraw`.
- `POST /api/v1/copies/import/preview` — CopiesCreate; action `ImportPreview`.
- `POST /api/v1/copies/import/confirm` — CopiesCreate; action `ImportConfirm`.
- `GET /api/v1/copies/export` — CopiesRead; action `Export`.

### Dashboard

- `GET /api/v1/dashboard/summary` — authenticated; action `GetSummary`.
- `GET /api/v1/dashboard/branches` — authenticated; action `GetBranches`.

### Employees

- `GET /api/v1/employees` — EmployeesRead; action `Get`.
- `GET /api/v1/employees/branches` — EmployeesRead; action `GetBranches`.
- `GET /api/v1/employees/{id:guid}` — EmployeesRead; action `GetById`.
- `POST /api/v1/employees` — EmployeesCreate; action `Create`.
- `PUT /api/v1/employees/{id:guid}` — EmployeesUpdate; action `Update`.
- `PATCH /api/v1/employees/{id:guid}/status` — EmployeesUpdate; action `UpdateStatus`.
- `DELETE /api/v1/employees/{id:guid}` — EmployeesDelete; action `Delete`.

### InventoryAudits

- `GET /api/v1/inventory-audits/locations` — InventoryAuditsRead; action `GetLocations`.
- `GET /api/v1/inventory-audits` — InventoryAuditsRead; action `Get`.
- `GET /api/v1/inventory-audits/{id:guid}` — InventoryAuditsRead; action `GetById`.
- `POST /api/v1/inventory-audits` — InventoryAuditsCreate; action `Create`.
- `POST /api/v1/inventory-audits/{id:guid}/start` — InventoryAuditsCreate; action `Start`.
- `POST /api/v1/inventory-audits/{id:guid}/scan` — InventoryAuditsScan; action `Scan`.
- `GET /api/v1/inventory-audits/{id:guid}/reconcile` — InventoryAuditsRead; action `Reconcile`.
- `POST /api/v1/inventory-audits/{id:guid}/complete` — InventoryAuditsComplete; action `Complete`.
- `POST /api/v1/inventory-audits/{id:guid}/apply` — InventoryAuditsApply; action `Apply`.
- `GET /api/v1/inventory-audits/{id:guid}/export` — InventoryAuditsExport; action `Export`.

### Kiosk

- `GET /api/v1/kiosk/books` — anonymous; action `Search`.
- `POST /api/v1/kiosk/books/semantic-search` — anonymous; action `SemanticSearch`.
- `GET /api/v1/kiosk/books/{id:guid}` — anonymous; action `GetBook`.

### Locations

- `GET /api/v1/locations` — LocationsRead; action `Get`.
- `GET /api/v1/locations/active-shelves` — LocationsRead; action `GetActiveShelves`.
- `GET /api/v1/locations/{type}/{id:guid}/impact` — LocationsRead; action `GetImpact`.
- `POST /api/v1/locations/branches` — LocationsCreate; action `CreateBranch`.
- `PUT /api/v1/locations/branches/{id:guid}` — LocationsUpdate; action `UpdateBranch`.
- `POST /api/v1/locations/areas` — LocationsCreate; action `CreateArea`.
- `PUT /api/v1/locations/areas/{id:guid}` — LocationsUpdate; action `UpdateArea`.
- `POST /api/v1/locations/shelves` — LocationsCreate; action `CreateShelf`.
- `PUT /api/v1/locations/shelves/{id:guid}` — LocationsUpdate; action `UpdateShelf`.
- `POST /api/v1/locations/branches/{id:guid}/activate` — LocationsUpdate; action `ActivateBranch`.
- `DELETE /api/v1/locations/branches/{id:guid}` — LocationsDeactivate; action `DeactivateBranch`.
- `DELETE /api/v1/locations/branches/{id:guid}/permanent` — LocationsDeactivate; action `DeleteBranch`.
- `POST /api/v1/locations/areas/{id:guid}/activate` — LocationsUpdate; action `ActivateArea`.
- `DELETE /api/v1/locations/areas/{id:guid}` — LocationsDeactivate; action `DeactivateArea`.
- `DELETE /api/v1/locations/areas/{id:guid}/permanent` — LocationsDeactivate; action `DeleteArea`.
- `POST /api/v1/locations/shelves/{id:guid}/activate` — LocationsUpdate; action `ActivateShelf`.
- `DELETE /api/v1/locations/shelves/{id:guid}` — LocationsDeactivate; action `DeactivateShelf`.
- `DELETE /api/v1/locations/shelves/{id:guid}/permanent` — LocationsDeactivate; action `DeleteShelf`.

### Me

- `GET /api/v1/me` — authenticated; action `Get`.
- `PATCH /api/v1/me/profile` — authenticated; action `Update`.
- `POST /api/v1/me/change-password` — authenticated; action `ChangePassword`.

### Members

- `GET /api/v1/members` — MembersRead; action `Get`.
- `GET /api/v1/members/{id:guid}` — MembersRead; action `GetById`.
- `GET /api/v1/members/{id:guid}/history` — MembersRead; action `History`.
- `POST /api/v1/members` — MembersCreate; action `Create`.
- `PUT /api/v1/members/{id:guid}` — MembersUpdate; action `Update`.
- `POST /api/v1/members/{id:guid}/card` — MembersManageCards; action `IssueCard`.
- `POST /api/v1/members/{id:guid}/card/renew` — MembersManageCards; action `RenewCard`.
- `PATCH /api/v1/members/{id:guid}/card/status` — MembersManageCards; action `CardStatus`.
- `POST /api/v1/members/{id:guid}/restrictions` — MembersManageRestrictions; action `Restrict`.
- `POST /api/v1/members/{id:guid}/restrictions/{restrictionId:guid}/remove` — MembersManageRestrictions; action `RemoveRestriction`.
- `POST /api/v1/members/{id:guid}/violations/{violationId:guid}/payments` — MembersManageFinances; action `Payment`.
- `POST /api/v1/members/{id:guid}/violations/{violationId:guid}/adjustments` — MembersManageFinances; action `Adjustment`.

### NotificationTemplates

- `GET /api/v1/notification-templates/events` — authenticated; action `GetEvents`.
- `GET /api/v1/notification-templates` — authenticated; action `GetAll`.
- `GET /api/v1/notification-templates/{id:guid}` — authenticated; action `GetById`.
- `POST /api/v1/notification-templates` — authenticated; action `Create`.
- `PUT /api/v1/notification-templates/{id:guid}` — authenticated; action `Update`.
- `DELETE /api/v1/notification-templates/{id:guid}` — authenticated; action `Delete`.

### Notifications

- `POST /api/v1/notifications/preview` — authenticated; action `Preview`.
- `POST /api/v1/notifications/send` — authenticated; action `Send`.
- `POST /api/v1/notifications/send-bulk` — authenticated; action `SendBulk`.
- `POST /api/v1/notifications/{id:guid}/retry` — authenticated; action `Retry`.
- `GET /api/v1/notifications/history` — authenticated; action `GetHistory`.
- `GET /api/v1/notifications/my` — authenticated; action `GetMyNotifications`.
- `GET /api/v1/notifications/my/{id:guid}` — authenticated; action `GetMyNotification`.
- `GET /api/v1/notifications/unread-count` — authenticated; action `GetUnreadCount`.
- `PUT /api/v1/notifications/{id:guid}/read` — authenticated; action `MarkRead`.
- `PUT /api/v1/notifications/read-all` — authenticated; action `MarkAllRead`.
- `GET /api/v1/notifications/recipients` — authenticated; action `SearchRecipients`.

### Payments

- `GET /api/v1/payments` — ViolationsRead; action `Get`.
- `GET /api/v1/payments/{id:guid}` — ViolationsRead; action `GetById`.
- `POST /api/v1/payments/preview` — ViolationsRead; action `Preview`.
- `POST /api/v1/payments` — ViolationsResolve; action `Create`.

### Permissions

- `GET /api/v1/permissions` — PermissionsRead; action `Get`.
- `GET /api/v1/permissions/modules` — PermissionsRead; action `GetModules`.
- `GET /api/v1/permissions/{id:guid}` — PermissionsRead; action `GetById`.
- `POST /api/v1/permissions` — PermissionsCreate; action `Create`.
- `PUT /api/v1/permissions/{id:guid}` — PermissionsUpdate; action `Update`.
- `DELETE /api/v1/permissions/{id:guid}` — PermissionsDelete; action `Delete`.

### Reports

- `GET /api/v1/reports/definitions` — authenticated; action `GetDefinitions`.
- `POST /api/v1/reports/preview` — authenticated; action `Preview`.
- `POST /api/v1/reports/export` — authenticated; action `Export`.
- `GET /api/v1/reports/{id:guid}/download` — authenticated; action `Download`.

### Reservations

- `GET /api/v1/reservations` — ReservationsRead; action `Get`.
- `GET /api/v1/reservations/{id:guid}` — ReservationsRead; action `GetById`.
- `POST /api/v1/reservations` — ReservationsCreate; action `Create`.
- `POST /api/v1/reservations/{id:guid}/cancel` — ReservationsCancel; action `Cancel`.
- `POST /api/v1/reservations/{id:guid}/fulfill` — ReservationsFulfill; action `Fulfill`.

### Roles

- `GET /api/v1/roles` — RolesRead; action `Get`.
- `GET /api/v1/roles/{id:guid}` — RolesRead; action `GetById`.
- `POST /api/v1/roles` — RolesCreate; action `Create`.
- `PUT /api/v1/roles/{id:guid}` — RolesUpdate; action `Update`.
- `DELETE /api/v1/roles/{id:guid}` — RolesDelete; action `Delete`.
- `PUT /api/v1/roles/{id:guid}/permissions` — RolesAssign; action `ReplacePermissions`.
- `PUT /api/v1/users/{userId:guid}/roles` — RolesAssign; action `ReplaceUserRoles`.

### SavedFilters

- `GET /api/v1/saved-filters` — authenticated; action `Get`.
- `POST /api/v1/saved-filters` — authenticated; action `Create`.
- `PUT /api/v1/saved-filters/{id:guid}` — authenticated; action `Update`.
- `DELETE /api/v1/saved-filters/{id:guid}` — authenticated; action `Delete`.

### Smtp

- `GET /api/v1/smtp` — SettingsRead; action `Get`.
- `POST /api/v1/smtp/test` — SettingsUpdate; action `Test`.

### StockReceipts

- `GET /api/v1/stock-receipts` — StockReceiptsRead; action `Get`.
- `GET /api/v1/stock-receipts/{id:guid}` — StockReceiptsRead; action `GetById`.
- `POST /api/v1/stock-receipts` — StockReceiptsCreate; action `Create`.
- `PUT /api/v1/stock-receipts/{id:guid}` — StockReceiptsUpdate; action `Update`.
- `GET /api/v1/stock-receipts/{id:guid}/confirmation` — StockReceiptsRead; action `GetConfirmation`.
- `POST /api/v1/stock-receipts/{id:guid}/confirm` — StockReceiptsConfirm; action `Confirm`.

### Suppliers

- `GET /api/v1/suppliers` — SuppliersRead; action `Get`.
- `GET /api/v1/suppliers/active` — SuppliersRead; action `GetActive`.
- `GET /api/v1/suppliers/{id:guid}` — SuppliersRead; action `GetById`.
- `POST /api/v1/suppliers` — SuppliersCreate; action `Create`.
- `PUT /api/v1/suppliers/{id:guid}` — SuppliersUpdate; action `Update`.
- `POST /api/v1/suppliers/{id:guid}/activate` — SuppliersUpdate; action `Activate`.
- `DELETE /api/v1/suppliers/{id:guid}` — SuppliersDeactivate; action `Deactivate`.

### SystemSettings

- `GET /api/v1/settings` — SettingsRead; action `Get`.
- `PUT /api/v1/settings/{key}` — SettingsUpdate; action `Update`.

### Users

- `GET /api/v1/users` — UsersRead; action `Get`.
- `GET /api/v1/users/{id:guid}` — UsersRead; action `GetById`.
- `POST /api/v1/users` — UsersCreate; action `Create`.
- `PUT /api/v1/users/{id:guid}` — UsersUpdate; action `Update`.
- `DELETE /api/v1/users/{id:guid}` — UsersDeactivate; action `Delete`.

### Violations

- `GET /api/v1/violations` — ViolationsRead; action `Get`.
- `GET /api/v1/violations/{id:guid}` — ViolationsRead; action `GetById`.
- `POST /api/v1/violations/preview` — ViolationsRead; action `PreviewFine`.
- `POST /api/v1/violations` — ViolationsCreate; action `Create`.
- `POST /api/v1/violations/{id:guid}/pay` — ViolationsResolve; action `Pay`.
- `POST /api/v1/violations/{id:guid}/waive` — ViolationsResolve; action `Waive`.

## Appendix B — Primary implementation map

```text
HTTP contracts/controllers
  Backend/src/UTH.Library.Api/Contracts/**
  Backend/src/UTH.Library.Api/Controllers/**

Application orchestration
  Backend/src/UTH.Library.Application/Features/**
  Backend/src/UTH.Library.Application/Abstractions/**

Domain state and invariants
  Backend/src/UTH.Library.Domain/Entities/**
  Backend/src/UTH.Library.Domain/Enums/**

Database queries and persistence
  Backend/src/UTH.Library.Infrastructure/Persistence/LibraryDbContext.cs
  Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/**
  Backend/src/UTH.Library.Infrastructure/Persistence/Configurations/**

Identity, sessions, roles and RSA/JWT
  Backend/src/UTH.Library.Infrastructure/Identity/**
  Backend/src/UTH.Library.Api/Authorization/**

Frontend authentication and permission UX
  Frontend/src/auth/**
  Frontend/src/routes/ProtectedRoute.tsx
  Frontend/src/app/navigation.ts
  Frontend/src/shared/auth/**
```
