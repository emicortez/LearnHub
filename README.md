# LearnHub

[![CI](https://github.com/emicortez/LearnHub/actions/workflows/ci.yml/badge.svg)](https://github.com/emicortez/LearnHub/actions/workflows/ci.yml)

An online course platform (Udemy-style) with a multi-agent AI tutor, built with .NET 10, .NET Aspire and Azure.

The goal is a production-grade system: designed for thousands of concurrent users, fully testable, deployed to Azure on a free-first budget, and developed like a team product (pull requests, CI, ADRs).

## Status

Early stage. The engineering foundation is in place:

| Area | State |
|---|---|
| Build conventions (SDK pin, shared props, Central Package Management, analyzers, warnings as errors) | ✅ |
| Aspire AppHost and shared service defaults (OpenTelemetry, health checks, resilience, service discovery) | ✅ |
| Catalog API with `/health` and `/alive`, covered by integration tests | ✅ |
| CI on every pull request: format, build, test | ✅ |
| Domain building blocks, Catalog domain, persistence | Next |

## Planned architecture

| Bounded context | Responsibility |
|---|---|
| Identity | Users, roles, authentication (OAuth2/OIDC) |
| Catalog | Courses, sections, lessons, instructors |
| Enrollment | Enrollments and payments (outbox, saga, idempotency) |
| Progress | Lesson progress and certificates |
| AI Tutor | RAG over course content with Microsoft Agent Framework |

Each service follows Clean Architecture with DDD (`Domain ← Application ← Infrastructure ← Api`) and communicates through HTTP or asynchronous messages (Wolverine).

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Minimal APIs, EF Core, Wolverine |
| Orchestration | .NET Aspire |
| Data | PostgreSQL, Redis |
| AI | Microsoft.Extensions.AI, Microsoft Agent Framework, vector search |
| Frontend | React, TypeScript |
| Cloud | Azure Container Apps, Bicep, `azd` |
| Quality | xUnit v3 (Microsoft.Testing.Platform), `WebApplicationFactory`, Testcontainers, GitHub Actions |
| Observability | OpenTelemetry |

## Getting started

Requirements: the .NET SDK pinned in [`global.json`](global.json) and Docker.

```bash
# Run the whole system with the Aspire dashboard
dotnet run --project src/AppHost/LearnHub.AppHost

# Run the tests
dotnet test --solution LearnHub.slnx
```

## Repository layout

```
src/
  AppHost/LearnHub.AppHost                 Aspire orchestrator
  BuildingBlocks/LearnHub.ServiceDefaults  Shared OpenTelemetry, health checks, resilience
  Services/Catalog/LearnHub.Catalog.Api    Catalog service
tests/
  Services/Catalog/LearnHub.Catalog.Api.Tests
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
