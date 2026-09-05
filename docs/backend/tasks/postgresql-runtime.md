# PostgreSQL runtime

## Scope

- Application runtime uses Npgsql/PostgreSQL exclusively.
- Development applies EF Core migrations automatically.
- Production migrations remain an explicit deployment operation.
- The PostgreSQL migration includes the complete Identity, role, permission, and refresh-token schema.
- Application runtime and integration tests use PostgreSQL exclusively.
- Integration tests run against an isolated PostgreSQL Testcontainer.
