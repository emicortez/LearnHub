# 0004. Repository per aggregate for writes, DbContext for reads

- Status: Accepted
- Date: 2026-10-17

## Context

EF Core's `DbContext` is already a Unit of Work and `DbSet` is already a repository. Wrapping them in a generic repository adds a layer that hides EF Core features without adding meaning. On the other hand, letting Application depend on EF Core directly for writes breaks the dependency rule ([ADR 0003](0003-clean-architecture-inside-each-service.md)) and lets code load partial aggregates.

## Decision

- **Writes:** one specific repository interface per aggregate root (for example `ICourseRepository` with `GetAsync` and `Add`), defined in Application and implemented in Infrastructure. It always loads the complete aggregate. There is no `Update` (the change tracker detects changes) and no `Save`.
- **Unit of Work:** `SaveChangesAsync` once per use case, through Wolverine's EF Core transactional middleware.
- **Reads:** queries use the `DbContext` directly with `AsNoTracking` and projection to DTOs, without a repository.
- **No generic repository.**

## Alternatives considered

- **Generic `IRepository<T>`:** leaks queryable abstractions, invites partial aggregate loads, and duplicates EF Core.
- **DbContext everywhere:** less code, but Application depends on infrastructure and handlers are harder to test.

## Consequences

- Write handlers are tested with a small fake; aggregates are always loaded whole.
- Read queries are fast and tailored to each screen.
- Two access styles coexist; this is intentional and follows [ADR 0005](0005-cqrs-level-2-without-event-sourcing.md).
