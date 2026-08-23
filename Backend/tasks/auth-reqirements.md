Act as a senior .NET security engineer and backend architect.

Extend the existing C# Clean Architecture solution with its first production-grade feature:

Authentication, role-based authorization, and permission-based authorization using JWT access tokens.

Before writing code:

1. Inspect the complete solution, project structure, target framework, package management, persistence setup, API style, error-handling conventions, and existing tests.
2. Preserve the current architecture and naming conventions.
3. Do not delete or overwrite unrelated code.
4. Present a concise implementation plan and proposed file tree.
5. Then implement the feature completely.
6. Do not stop after generating examples or pseudocode.
7. Build the solution, run tests, and fix all failures.

PROJECT ASSUMPTIONS

Unless the existing solution indicates otherwise:

- Use the latest installed .NET LTS version.
- Use ASP.NET Core.
- Use PostgreSQL and Entity Framework Core.
- Use ASP.NET Core Identity for local user accounts, password hashing, security stamps, roles, lockout, and account tokens.
- Use JWT Bearer authentication for API access.
- Use asymmetric JWT signing, preferably RSA with RS256.
- Use short-lived JWT access tokens.
- Use opaque, cryptographically random refresh tokens.
- Use role-based access control combined with fine-grained permissions.
- Use policy-based authorization for permission checks.
- Use Minimal APIs if the project already uses Minimal APIs; otherwise preserve its existing controller style.
- Follow the existing Result, validation, mediator, and mapping conventions. Do not introduce new libraries if equivalent mechanisms already exist.

IMPORTANT SECURITY DECISION

This application is acting as a first-party authentication system, not a complete OAuth or OpenID Connect authorization server.

Do not invent OAuth or OpenID Connect protocol endpoints.

If the application later needs third-party clients, social login, enterprise SSO, authorization-code flow, or federation, document that it should integrate a standards-compliant identity provider such as Microsoft Entra ID, Auth0, Keycloak, Duende IdentityServer, or OpenIddict instead of extending this custom token implementation.

CLEAN ARCHITECTURE RULES

Preserve these dependency directions:

- Domain references no other project.
- Application references Domain only.
- Infrastructure references Application and Domain.
- Api references Application and Infrastructure.
- Domain and Application must not reference:
  - ASP.NET Core Identity
  - Entity Framework Core
  - JwtBearer
  - HTTP abstractions
  - Infrastructure implementations

Place abstractions in Application and implementations in Infrastructure.

Suggested responsibilities:

Domain:
- Business concepts that are independent of ASP.NET Core Identity.
- Permission definitions or strongly typed permission names, if appropriate.
- Domain-specific authorization rules only when they are true business rules.

Application:
- Authentication and user-management use cases.
- IIdentityService
- ITokenService
- ICurrentUser
- IPermissionService, if required
- Authentication result models
- Commands, queries, validation, and application errors
- No IdentityUser, UserManager, SignInManager, DbContext, or JWT implementation types

Infrastructure:
- ApplicationUser and ApplicationRole
- ASP.NET Core Identity configuration
- EF Core mappings and migrations
- IdentityService implementation
- JWT creation
- Refresh-token creation, hashing, rotation, and persistence
- Permission persistence
- Role and permission management
- Email-token generation infrastructure, even if the email sender is initially a development implementation

Api:
- Authentication endpoints
- Authorization policies and handlers
- Current-user HTTP adapter
- Request and response contracts
- Secure cookie handling
- Rate limiting
- Problem Details mapping

AUTHENTICATION DATA MODEL

Use ASP.NET Core Identity with Guid primary keys unless the existing solution already uses a different identifier type.

Create or extend the following persistence models:

ApplicationUser:
- Id
- Email
- UserName
- DisplayName
- IsActive
- CreatedAtUtc
- LastLoginAtUtc
- Identity security and lockout fields

ApplicationRole:
- Id
- Name
- Description
- IsSystemRole
- CreatedAtUtc

Permission:
- Id
- Name
- Description
- Module
- CreatedAtUtc

RolePermission:
- RoleId
- PermissionId

RefreshTokenSession:
- Id
- UserId
- TokenHash
- FamilyId
- ParentTokenId, nullable
- CreatedAtUtc
- ExpiresAtUtc
- UsedAtUtc, nullable
- RevokedAtUtc, nullable
- ReplacedByTokenId, nullable
- CreatedByIp, nullable
- RevokedByIp, nullable
- UserAgent, nullable
- RevocationReason, nullable

Never persist a raw refresh token. Store only a one-way cryptographic hash.

Add appropriate:

- Primary keys
- Unique indexes
- Foreign keys
- Maximum lengths
- Cascade behaviors
- Concurrency protection
- Indexes for token lookup, user lookup, expiration cleanup, and role-permission lookup

PERMISSION MODEL

Implement roles as collections of permissions.

Use stable permission names following this convention:

<resource>.<action>

Initial permissions:

- users.read
- users.create
- users.update
- users.deactivate
- roles.read
- roles.create
- roles.update
- roles.assign
- permissions.read
- todos.read
- todos.create
- todos.update
- todos.delete

Create centralized permission constants or a strongly typed catalog. Do not scatter string literals throughout the codebase.

Add initial system roles:

Administrator:
- Contains every permission.

User:
- Contains the normal Todo permissions appropriate for a regular user.
- Must not contain user-management or role-management permissions.

Seed roles and permissions idempotently. Never hard-code a production administrator password. If development admin seeding is supported, read credentials from user secrets or environment variables and disable it by default outside Development.

AUTHENTICATION ENDPOINTS

Implement versioned endpoints consistent with the existing API conventions:

POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/logout-all
GET  /api/v1/auth/me

Also create secure application use cases and extension points for:

POST /api/v1/auth/confirm-email
POST /api/v1/auth/resend-confirmation
POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
POST /api/v1/auth/change-password

Implement email confirmation and password reset if the existing project has an email abstraction. Otherwise implement the application flow and a development-only email sender that logs only the delivery destination and a safe local testing URL. Never log passwords, JWTs, refresh tokens, reset tokens, or confirmation tokens in Production.

REGISTRATION

Registration must:

- Normalize and validate email.
- Validate password using ASP.NET Core Identity.
- Avoid custom password hashing.
- Avoid revealing sensitive account state.
- Assign only the default User role.
- Never allow the client to submit roles or permissions.
- Require confirmed email before login unless explicitly disabled through development configuration.
- Return a safe response that does not expose internal Identity errors unnecessarily.

LOGIN

Login must:

- Use ASP.NET Core Identity password verification.
- Respect user-disabled status.
- Respect email-confirmation requirements.
- Enable lockout on failed password attempts.
- Update LastLoginAtUtc only after successful authentication.
- Return generic invalid-credentials responses to reduce account enumeration.
- Issue an access token and establish a refresh-token session.
- Avoid revealing whether the email exists, the password is wrong, or the account is disabled.
- Apply endpoint-specific rate limiting.

JWT ACCESS TOKEN

Access tokens must:

- Be signed using an asymmetric key, preferably RS256.
- Have a short configurable lifetime; default to 10 minutes.
- Contain only required claims.
- Never contain secrets or sensitive personal information.
- Include:
  - sub: immutable user ID
  - jti: unique token identifier
  - iss: configured issuer
  - aud: configured audience
  - iat
  - nbf
  - exp
  - email only if the application genuinely requires it
  - role claims
  - permission claims
- Use one documented claim name for permissions, such as "permission".
- Not use email as the user identifier.
- Not accept an algorithm supplied by the token itself without server-side restrictions.

Configure JWT Bearer validation to require:

- Valid signature
- Expected issuer
- Expected audience
- Token lifetime
- Expiration
- Signed tokens
- Restricted signing algorithm
- Small configurable clock skew, preferably no more than 30 seconds

Return:

- 401 when authentication is missing or invalid.
- 403 when the authenticated user lacks permission.

Do not return internal token-validation details to clients.

JWT SIGNING KEYS

Do not commit signing keys or private keys.

Use configuration abstractions that support:

- Development keys from user secrets or mounted local files
- Production keys from a secret manager or key-management service
- A key identifier using the JWT kid header
- Future key rotation
- Multiple public validation keys during rotation

Fail fast at startup when Production signing configuration is missing or insecure.

Do not silently generate a new signing key on every application restart.

REFRESH TOKENS

Refresh tokens must:

- Be opaque random values generated using RandomNumberGenerator.
- Have at least 256 bits of entropy.
- Never be JWTs.
- Never be stored in plaintext.
- Be stored as a cryptographic hash.
- Have a configurable absolute expiration.
- Be bound to a user and token family.
- Be single-use.
- Be rotated atomically on every refresh.
- Invalidate the previous token during rotation.
- Detect reuse of an already-used or revoked refresh token.
- Revoke the complete token family when reuse is detected.
- Be revoked on logout.
- Revoke all of the user’s token families on logout-all.
- Be revoked after password reset, password change, account deactivation, or other relevant security events.
- Be cleaned up after expiration according to a documented retention policy.

Handle concurrent refresh requests safely using a database transaction and concurrency protection. Only one request using a refresh token may succeed.

Do not issue a replacement access token if refresh-token rotation fails.

BROWSER TOKEN TRANSPORT

Assume the primary client is a browser SPA unless existing project requirements say otherwise.

- Return the access token in the response body.
- The client should keep the access token in memory.
- Put the refresh token in an HttpOnly cookie.
- Configure the refresh cookie with:
  - HttpOnly = true
  - Secure = true outside local HTTP development
  - SameSite based on the actual frontend/API topology
  - Restricted Path covering only refresh/logout endpoints where practical
  - Explicit expiration
- Do not store access tokens or refresh tokens in localStorage or sessionStorage.
- Do not expose the refresh token to browser JavaScript.
- Protect cookie-backed refresh and logout operations from CSRF.
- Validate trusted Origin/Referer values where appropriate.
- Use an antiforgery strategy if cross-site cookie usage is required.
- Do not use a wildcard CORS origin with credentials.

If this API is intended for mobile or machine clients, separate the token transport behavior behind configuration and document secure platform storage requirements.

AUTHORIZATION

Implement three layers:

1. Authenticated user
2. Roles
3. Fine-grained permissions

Use ASP.NET Core policy-based authorization.

Create:

- PermissionRequirement
- PermissionAuthorizationHandler
- A policy naming convention such as "Permission:<permission-name>"
- A dynamic IAuthorization