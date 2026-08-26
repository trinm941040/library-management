# PostgreSQL runtime

## Scope

- Application runtime uses Npgsql/PostgreSQL exclusively.
- Development applies EF Core migrations automatically.
- Production migrations remain an explicit deployment operation.
- The PostgreSQL migration includes the complete Identity, role, permission, and refresh-token schema.
- SQLite is isolated to integration tests and is not referenced by the application runtime project.
