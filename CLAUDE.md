# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Default AI Workflow

Use the Gentle-Orchestrator workflow by default for this project: coordinate first, delegate substantial implementation/review work to focused sub-agents, keep the main conversation thread thin, and synthesize results back to the user.

## Commands

```bash
# Run the whole system (Aspire dashboard)
dotnet run --project src/AppHost/LearnHub.AppHost

# Same checks as CI (.github/workflows/ci.yml)
dotnet restore LearnHub.slnx
dotnet format LearnHub.slnx --verify-no-changes --no-restore
dotnet build LearnHub.slnx --configuration Release --no-restore
dotnet test --solution LearnHub.slnx --configuration Release --no-build

# Fix formatting
dotnet format LearnHub.slnx

# Run a subset of tests (Microsoft.Testing.Platform / xUnit v3 filters)
dotnet test --solution LearnHub.slnx --filter-class "*HealthEndpointsTests"
```

Tests run on **Microsoft.Testing.Platform** (`global.json` → `"test": { "runner": "Microsoft.Testing.Platform" }`); VSTest is not used. Test projects have `OutputType Exe` and do not reference `Microsoft.NET.Test.Sdk`.

**NuGet**: a local `NuGet.config` at the root restricts sources to `nuget.org` only. The global user config includes a Telerik feed that requires credentials and breaks restores — the local config overrides it. Never remove `NuGet.config`.

## Build conventions

- `global.json` pins the SDK (10.0.4xx, `latestFeature`).
- `Directory.Build.props`: `net10.0`, nullable, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`. A warning breaks the build — fix it, do not suppress it without a comment explaining why.
- `Directory.Packages.props`: Central Package Management with transitive pinning. Versions go only here; `PackageReference` never has `Version` (NU1008).
- `.editorconfig` is enforced at build: LF line endings, file-scoped namespaces. `CA1707` is disabled only under `tests/**` (underscored test names).

## Architecture

**Current layout**

```
src/AppHost/LearnHub.AppHost                 Aspire AppHost (Aspire.AppHost.Sdk)
src/BuildingBlocks/LearnHub.ServiceDefaults  AddServiceDefaults() / MapDefaultEndpoints(): OpenTelemetry, health checks (/health, /alive), service discovery, HTTP resilience
src/Services/Catalog/LearnHub.Catalog.Api    Catalog service (only health endpoints so far)
tests/Services/Catalog/LearnHub.Catalog.Api.Tests  WebApplicationFactory integration tests
```

Every API project references ServiceDefaults and calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. The AppHost registers every service and infrastructure resource; run the AppHost, not individual services.

**Target per-service layout** (Clean Architecture, added as each service grows):

```
LearnHub.{Service}.Domain/          # Zero external dependencies — only .NET BCL
LearnHub.{Service}.Application/     # Wolverine handlers, validators
LearnHub.{Service}.Infrastructure/  # EF Core, repositories, external service implementations
LearnHub.{Service}.Api/             # Minimal API endpoints, DI wiring, Program.cs
```

Dependency rule: `Domain ← Application ← Infrastructure ← Api`. Services communicate through HTTP (sync) or Wolverine messaging (async). MediatR, MassTransit and AutoMapper are not used (commercial licenses).

## Domain Rules

These invariants apply once domain projects exist — do not break them:

- **Domain has zero external dependencies.** If a `*.Domain` class needs a NuGet package, that is a design mistake.
- **Business logic lives in the domain.** Invariants belong on entities and value objects, not in Application or Infrastructure.
- **Aggregates are created through factory methods** (`Course.Create(...)`). The private parameterless constructor exists only for EF Core.
- **Domain events are raised inside aggregates and dispatched by Infrastructure** after saving, then cleared.
- **Value objects are immutable and equal by value.**
- **Error codes follow `{service}.{entity}.{problem}`** (e.g. `catalog.course.title_required`).

## Workflow

- Trunk-based: short branches from `main`, Conventional Commits, squash merge after green CI. See `CONTRIBUTING.md`.
- Test-first for behavior changes.
- Generated artifacts (code, comments, docs, commits, PRs) are in English.
- `tutoriales/`, `odd/` and other personal material are gitignored and must never be committed.
