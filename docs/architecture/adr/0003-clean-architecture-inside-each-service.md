# 0003. Clean Architecture inside each service

- Status: Accepted
- Date: 2026-10-17

## Context

Business rules must stay testable without databases or frameworks, and infrastructure choices (EF Core, Wolverine, the LLM provider) must be replaceable without touching them.

## Decision

Each service is split into projects following the dependency rule `Domain ← Application ← Infrastructure ← Api`:

- **Domain:** aggregates, value objects, and domain events; depends only on the .NET base class library and `LearnHub.SharedKernel`.
- **Application:** use cases as Wolverine handlers, organized by feature (vertical slices inside the layer).
- **Infrastructure:** EF Core, repositories, and adapters for external services.
- **Api:** Minimal API endpoints and composition root.

Layers are added as a service grows; a service with no behavior yet does not get empty projects.

## Alternatives considered

- **Vertical Slice Architecture alone:** less ceremony, but the dependency rule becomes a convention instead of a compiler-checked boundary.
- **Single project per service with folders:** nothing prevents domain code from referencing infrastructure.

## Consequences

- The domain is unit-testable in isolation, and the boundary is checked by the compiler and by [fitness functions](0008-hand-written-architecture-fitness-functions.md).
- More projects and some mapping between layers; for trivial CRUD this is overhead.
