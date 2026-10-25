# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Default AI Workflow

Use the Gentle-Orchestrator workflow by default for this project: coordinate first, delegate substantial implementation/review work to focused sub-agents, keep the main conversation thread thin, and synthesize results back to the user.

## Commands

```bash
# Run the whole system (Aspire dashboard)
dotnet run --project src/AppHost/LearnHub.AppHost
# In Development each API serves /openapi/v1.json and an interactive UI at /scalar

# Same checks as CI (.github/workflows/ci.yml)
dotnet restore LearnHub.slnx --locked-mode
dotnet format LearnHub.slnx --verify-no-changes --no-restore
dotnet build LearnHub.slnx --configuration Release --no-restore
dotnet test --solution LearnHub.slnx --configuration Release --no-build

# Fix formatting
dotnet format LearnHub.slnx

# Run a subset of tests (Microsoft.Testing.Platform / xUnit v3 filters)
dotnet test --solution LearnHub.slnx --filter-class "*HealthEndpointsTests"
```

Tests need **Docker** running: `LearnHub.Catalog.Infrastructure.Tests` starts a PostgreSQL container (Testcontainers), and the AppHost runs PostgreSQL as a container too.

Tests run on **Microsoft.Testing.Platform** (`global.json` → `"test": { "runner": "Microsoft.Testing.Platform" }`); VSTest is not used. Test projects have `OutputType Exe` and do not reference `Microsoft.NET.Test.Sdk`.

**NuGet**: a local `NuGet.config` at the root restricts sources to `nuget.org` only. The global user config includes a Telerik feed that requires credentials and breaks restores — the local config overrides it. Never remove `NuGet.config`.

## Build conventions

- `global.json` pins the SDK (10.0.4xx, `latestFeature`).
- `Directory.Build.props`: `net10.0`, nullable, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`. A warning breaks the build — fix it, do not suppress it without a comment explaining why.
- `Directory.Packages.props`: Central Package Management with transitive pinning. Versions go only here; `PackageReference` never has `Version` (NU1008).
- Supply chain:
  - lock files (`packages.lock.json`, committed; CI restores with `--locked-mode`) in every project except the AppHost, whose Aspire packages are per-OS;
  - NuGet Audit `all`/`moderate`, so a moderate or higher advisory fails the build;
  - package source mapping in `NuGet.config`;
  - Dependabot weekly for NuGet and Actions;
  - Actions pinned by SHA.

  Secrets never go in the repo; where they live per environment is in `CONTRIBUTING.md`.
- `.editorconfig` is enforced at build: LF line endings, file-scoped namespaces. `CA1707` is disabled only under `tests/**` (underscored test names).

## Architecture

**Current layout**

```
src/AppHost/LearnHub.AppHost                 Aspire AppHost (Aspire.AppHost.Sdk)
src/BuildingBlocks/LearnHub.ServiceDefaults  AddServiceDefaults() / MapDefaultEndpoints(): OpenTelemetry, health checks (/health, /alive), service discovery, HTTP resilience, forwarded headers, security headers
src/BuildingBlocks/LearnHub.SharedKernel     Entity, AggregateRoot, ValueObject, Result, Error
src/Services/Catalog/LearnHub.Catalog.Domain          Course aggregate, value objects, domain events
src/Services/Catalog/LearnHub.Catalog.Application     Use-case handlers (plain classes until Wolverine), ICourseRepository
src/Services/Catalog/LearnHub.Catalog.Infrastructure  In-memory repository (replaced by EF Core in module 6); PostgreSQL schema in Persistence/Schema/*.sql, read queries in Persistence/Queries/*.sql
src/Services/Catalog/LearnHub.Catalog.Api             Minimal API endpoints by feature (Courses/CourseEndpoints.cs)
tests/Services/Catalog/LearnHub.Catalog.Api.Tests     WebApplicationFactory integration tests
tests/Services/Catalog/LearnHub.Catalog.Infrastructure.Tests  Schema and query tests against a real PostgreSQL container (Testcontainers)
tests/LearnHub.Architecture.Tests                     Dependency, encapsulation, layer and service-isolation rules
```

Every API project references ServiceDefaults and calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. The AppHost registers every service and infrastructure resource; run the AppHost, not individual services.

**Target per-service layout** (Clean Architecture, added as each service grows):

```
LearnHub.{Service}.Domain/          # Zero external dependencies — only .NET BCL
LearnHub.{Service}.Application/     # Wolverine handlers, validators
LearnHub.{Service}.Infrastructure/  # EF Core, repositories, external service implementations
LearnHub.{Service}.Api/             # Minimal API endpoints, DI wiring, Program.cs
```

**Persistence**: the AppHost runs one PostgreSQL server (`postgres`, image `postgres:18.3`, data volume) with one database per service (ADR 0002); Catalog's is `catalog`, passed to `catalog-api` as `ConnectionStrings:catalog`. Catalog's schema is versioned SQL in `LearnHub.Catalog.Infrastructure/Persistence/Schema/` (applied by the schema tests; EF Core migrations take over in module 6 and must reproduce it):
- every constraint is named (`pk_`, `uq_`, `fk_`, `ck_`), so a violation reports which rule failed through its SQLSTATE (`23505`, `23514`, `23503`) and constraint name;
- money is unconstrained `numeric` with explicit range and scale checks, because `numeric(12, 2)` silently rounds extra decimals;
- `lessons.course_id` cascades on delete (lessons belong to the aggregate);
- no foreign key ever crosses services or points at the identity provider (`instructor_id` is the Entra `oid`).
- the read queries in `Persistence/Queries/` (public listing, instructor's courses, published course outline, longest courses per instructor) are the results the EF Core repository must reproduce: a course without lessons has 0 lessons and 0 seconds (`LEFT JOIN`, `count(l.id)`, `coalesce(sum(...), 0)`), totals are `bigint`, and order is by title then the unique slug. Their tests run on a database of their own (`CreateEmptyDatabaseAsync`), since they read whole tables.
The schema tests use the same image as the AppHost; keep them in sync.

**Authentication** is delegated to Microsoft Entra External ID (ADR 0009). Each API is a resource server: `AddJwtBearer` reads `Authentication:Schemes:Bearer` (Authority, ValidAudiences), accepts only RS256, and takes the user id from the `oid` claim (Entra's `sub` is pairwise per app). Writes require the `Instructor` policy (`Authentication/CatalogPolicies.cs`): a delegated token (with `scp`) carrying the `courses.write` scope and the `instructor` app role (`roles` claim). Ownership is a business rule checked in the use case (`PublishCourse.RequestedBy`), returning `Error.Forbidden` → 403. Reads are public. API tests never call the provider: `TestTokens` issues tokens signed with a test key, and `CatalogApiFactory` trusts that key.

**Browser access** goes through a same-origin BFF (ADR 0010), so APIs never authenticate cookies and need no antiforgery. CORS is closed by default: `Cors:AllowedOrigins` lists exact origins only (validated at startup, never `*`), without credentials, and `UseCors` runs before authentication, authorization and rate limiting.

**Security headers** come from `ServiceDefaults` (`SecurityHeadersStartupFilter`) on every response, errors included: `nosniff`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (except `/scalar` in Development), `X-Frame-Options: DENY`, and `Cache-Control: no-store` when the request carries `Authorization` or the response is 401/403. Kestrel sends no `Server` header. HSTS (`max-age=31536000`) is sent by the app on https requests outside Development and loopback, since the Container Apps ingress cannot add headers; behind the ingress the forwarded headers restore https first. The obsolete `X-XSS-Protection` is never sent.

Dependency rule: `Domain ← Application ← Infrastructure ← Api`. Services communicate through HTTP (sync) or Wolverine messaging (async), never through project references to another service (enforced by `ServiceIsolationTests`). MediatR, MassTransit and AutoMapper are not used (commercial licenses).

## Domain Rules

These invariants apply once domain projects exist — do not break them:

- **Domain has zero external dependencies.** If a `*.Domain` class needs a NuGet package, that is a design mistake. Enforced by `tests/LearnHub.Architecture.Tests`: register every new domain assembly in both `DomainDependencyTests` and `DomainEncapsulationTests`.
- **Business logic lives in the domain.** Invariants belong on entities and value objects, not in Application or Infrastructure.
- **Aggregates are created through factory methods** (`Course.Create(...)`). The private parameterless constructor exists only for EF Core. Entities and value objects have no public constructors and no public setters — enforced by `DomainEncapsulationTests`.
- **Domain events are raised inside aggregates and dispatched by Infrastructure** after saving, then cleared.
- **Value objects are immutable and equal by value.**
- **Error codes follow `{service}.{entity}.{problem}`** (e.g. `catalog.course.title_required`) and every error has a type, created through `Error.Validation`, `Error.NotFound`, `Error.Conflict` or `Error.Forbidden`. The Api maps the type to 400, 404, 409 or 403 in `Errors/ErrorResults.cs` (Problem Details with a `code` extension); unexpected exceptions become a generic 500 through `UnhandledExceptionHandler`.

## Workflow

- Trunk-based: short branches from `main`, Conventional Commits, squash merge after green CI. See `CONTRIBUTING.md`.
- Test-first for behavior changes.
- `docs/product/vision.md` is the product north star: learning by doing comes first; build features when the curriculum reaches them.
- Architecture docs live in `docs/architecture/` (quality attributes, context map, C4, data model). Read the ADRs in `docs/architecture/adr/` before changing a recorded decision; record new significant decisions as a new ADR, and supersede rather than edit accepted ones.
- Generated artifacts (code, comments, docs, commits, PRs) are in English.
- `tutoriales/`, `odd/` and other personal material are gitignored and must never be committed.
