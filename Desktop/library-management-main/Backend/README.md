# LibraryManegement

Clean Architecture REST API for the UTH library domain, targeting .NET 10. Local development uses SQLite; PostgreSQL remains available for container and production-like workflows.

## Structure

```text
Domain <- Application <- Infrastructure <- Api
Tests reference only the projects they verify.
```

The initial vertical slice is `Todos`. Domain rules stay in `TodoItem`; application use cases depend on `ITodoRepository`; EF Core and PostgreSQL are isolated in Infrastructure; HTTP contracts are isolated in Api. Authentication is intentionally not implemented, with authorization registration left as the extension point.

## Requirements

- .NET SDK 10.0+
- Docker Desktop for the container workflow
- SQLite is used automatically in the Development environment and stores data in `library-development.db`.
- Docker Desktop for the PostgreSQL container workflow

## Commands

```bash
dotnet restore LibraryManegement.sln
dotnet build LibraryManegement.sln
dotnet test LibraryManegement.sln
dotnet run --project src/UTH.Library.Api
```

The Development configuration uses SQLite and creates the schema automatically. Override `Database__Provider` with `Postgres` and set `ConnectionStrings__LibraryDatabase` when running against PostgreSQL; do not commit credentials.

## Migrations

```bash
dotnet ef migrations add InitialCreate \
  --project src/UTH.Library.Infrastructure \
  --startup-project src/UTH.Library.Api \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project src/UTH.Library.Infrastructure \
  --startup-project src/UTH.Library.Api
```

## Docker

```bash
docker compose up --build
curl http://localhost:8080/health
```

Run migration commands for PostgreSQL with the connection string pointed at `localhost`, or run them from a one-off SDK container before using the API. SQLite development uses `EnsureCreated` and does not use the PostgreSQL migration set.

OpenAPI is available at `/openapi/v1.json`; health is available at `/health`.