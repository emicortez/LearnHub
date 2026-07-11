# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the full stack (all services + infrastructure via Aspire dashboard)
dotnet run --project src/AppHost/LearnHub.AppHost

# Build a specific service
dotnet build src/Services/Identity/Identity.Domain
dotnet build src/Services/Identity/Identity.API

# Build everything
dotnet build LearnHub.sln

# Run tests (once added in Week 7)
dotnet test                                        # all tests
dotnet test --filter "FullyQualifiedName~Identity" # single service
dotnet test --filter "Category=Unit"               # unit tests only
```

**NuGet**: a local `NuGet.config` at the root restricts sources to `nuget.org` only. The global user config includes a Telerik feed that requires credentials and breaks restores — the local config overrides it. Never remove `NuGet.config`.

## Architecture

**Microservices** — each service under `src/Services/{Name}/` is independent with its own DB. Services communicate via HTTP/gRPC (sync) or RabbitMQ/MassTransit (async events).

**Per-service layout** (Clean Architecture):
```
{Service}.Domain/          # Zero external dependencies — only .NET BCL
{Service}.Application/     # MediatR handlers, FluentValidation validators
{Service}.Infrastructure/  # EF Core, repositories, external service impls
{Service}.API/             # Minimal API endpoints, DI wiring, Program.cs
```

The dependency rule is strict: `Domain ← Application ← Infrastructure ← API`. Nothing in Domain references NuGet packages.

**AppHost** (`src/AppHost/LearnHub.AppHost`) is the .NET Aspire orchestrator. It registers all services and infrastructure (PostgreSQL, Redis, RabbitMQ). Run this project to start the entire stack locally — not individual services.

**ServiceDefaults** (`src/ServiceDefaults/LearnHub.ServiceDefaults`) contains shared Aspire configuration: OpenTelemetry, health checks, service discovery. Every `*.API` project references it.

## Domain Rules

These are invariants enforced across all services — do not break them:

- **Domain has zero external dependencies.** If a new class in `*.Domain` needs a NuGet package, that's a design mistake. Move the dependency to Application or Infrastructure.

- **Never put business logic in Application or Infrastructure.** Logic that protects invariants (e.g., email format, password length) belongs on the domain entity or value object.

- **Aggregates only via factory methods.** Never `new User(...)` directly — always `User.Create(...)`. The private parameterless constructor exists only for EF Core.

- **`IPasswordHasher` lives in Domain; BCrypt lives in Infrastructure.** The Domain defines the contract. The entity receives an already-hashed password — it never hashes inline.

- **Domain Events are raised inside Aggregates, dispatched by Infrastructure.** After saving, Infrastructure reads `aggregate.DomainEvents`, dispatches them via MediatR, then calls `ClearDomainEvents()`.

- **Value Objects are immutable and equal by value.** `Email`, `UserId`, etc. inherit `ValueObject` and implement `GetEqualityComponents()`. Setters are forbidden.

- **Error codes follow `{service}.{entity}.{problem}` format** (e.g., `user.email.invalid_format`). All codes for an entity live in `{Service}.Domain/Errors/{Entity}Errors.cs`.

## Current State

Identity Service is in progress (Week 1 of 18). Only `Identity.Domain` is built — Application, Infrastructure, and API layers contain scaffolding only. `Identity.API/Program.cs` still has the default Aspire template code; it will be replaced when the API layer is implemented.

Test projects do not exist yet (planned for Week 7). Strict TDD mode will activate once xUnit + Testcontainers are added.
