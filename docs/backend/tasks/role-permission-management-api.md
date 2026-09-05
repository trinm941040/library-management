# Role and permission management API

## Goal

Provide permission-protected APIs for managing roles, permissions, role-permission assignments, and user-role assignments.

## Endpoints

- `GET /api/v1/roles?search=`
- `GET /api/v1/roles/{id}`
- `POST /api/v1/roles`
- `PUT /api/v1/roles/{id}`
- `DELETE /api/v1/roles/{id}`
- `PUT /api/v1/roles/{id}/permissions`
- `PUT /api/v1/users/{userId}/roles`
- `GET /api/v1/permissions?search=&module=`
- `GET /api/v1/permissions/{id}`
- `POST /api/v1/permissions`
- `PUT /api/v1/permissions/{id}`
- `DELETE /api/v1/permissions/{id}`

## Security decisions

- Every endpoint requires its matching `roles.*` or `permissions.*` policy.
- An authenticated Administrator inherits the global permission-policy bypass.
- Role-permission and user-role updates replace the complete assignment atomically.
- System roles cannot be deleted or renamed.
- Permissions declared by the application cannot be deleted, renamed, or moved to another module.
- The last active Administrator cannot lose the Administrator role.
- Changing a user's roles revokes active refresh tokens and rotates the Identity security stamp.
- A custom permission name uses lowercase `module.action` format and its module must match the prefix.

## Acceptance criteria

- [x] Application contracts do not expose EF Core or ASP.NET Core Identity types.
- [x] Role and permission CRUD APIs are implemented.
- [x] Role-permission and user-role assignment APIs are implemented.
- [x] Protected system resources cannot be removed accidentally.
- [x] Authorization and management behavior have integration coverage.
