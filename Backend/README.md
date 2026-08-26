# LibraryManegement

Clean Architecture REST API for the UTH library domain, targeting .NET 10 and PostgreSQL.

## Structure

```text
Domain <- Application <- Infrastructure <- Api
Tests reference only the projects they verify.
```

The initial vertical slice is `Todos`. Domain rules stay in `TodoItem`; application use cases depend on `ITodoRepository`; EF Core and PostgreSQL are isolated in Infrastructure; HTTP contracts are isolated in Api. Authentication is intentionally not implemented, with authorization registration left as the extension point.

## Requirements

- .NET SDK 10.0+
- PostgreSQL 17+ or Docker Desktop

## Commands

```bash
dotnet restore LibraryManegement.sln
dotnet build LibraryManegement.sln
dotnet test LibraryManegement.sln
dotnet run --project src/UTH.Library.Api
```

Development connects to PostgreSQL at `localhost:5432` and automatically applies EF Core migrations. Override `ConnectionStrings__LibraryDatabase` for another environment; do not commit production credentials.

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

Development applies migrations on startup. For production, keep `Database__MigrateOnStartup=false` and run `dotnet ef database update` as a controlled deployment step.

Integration tests use an isolated PostgreSQL Testcontainer and therefore require a running Docker daemon.

OpenAPI is available at `/openapi/v1.json`; health is available at `/health`.
