# 0002. Microservices with a database per service

- Status: Accepted
- Date: 2026-10-17

## Context

LearnHub is built by one developer, its domain is still being discovered, the budget is at most USD 20 per month, and maintainability is the top [quality attribute](../quality-attributes.md). On those forces alone, a modular monolith is the better fit.

There is one more driver: LearnHub is a learning and portfolio project whose explicit goal is real practice with distributed systems (messaging, outbox, sagas, observability across services). A modular monolith does not exercise those skills.

## Decision

We split LearnHub into a small number of services aligned with the bounded contexts in the [context map](../context-map.md): Catalog, Enrollment, Progress, and AI Tutor, plus Identity as a generic subdomain.

To contain the cost:

- One PostgreSQL server with **one database per service**; no service reads another service's database.
- .NET Aspire orchestrates local development.
- Observability (OpenTelemetry) from day one.
- Asynchronous communication by default ([ADR 0006](0006-asynchronous-messaging-by-default.md)) to avoid a distributed monolith.

## Alternatives considered

- **Modular monolith:** simpler to operate, debug, and deploy, and the textbook choice for this context. Rejected only because it does not serve the learning driver.
- **Shared database across services:** cheaper, but couples services through the schema and creates a distributed monolith.

## Consequences

- Operational complexity, eventual consistency, and harder debugging are accepted costs that a monolith would not have.
- Services can evolve and deploy independently.
- **Revisit signal:** if two services keep changing together, merge them.
