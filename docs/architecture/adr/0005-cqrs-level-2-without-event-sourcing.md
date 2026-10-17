# 0005. CQRS level 2 without Event Sourcing

- Status: Accepted
- Date: 2026-10-17

## Context

Writes need aggregates that protect invariants; reads (most of the traffic) need flat projections tailored to each screen. Some reads, such as course search with ratings and enrollment counts, combine data from several bounded contexts. No LearnHub context treats its history as the core of the business.

## Decision

- **CQRS level 2 in every service:** separate command and query handlers over the same database. Commands go through aggregates and repositories ([ADR 0004](0004-repository-per-aggregate-for-writes.md)); queries use projections.
- **Level 3 (separate read models updated by events) only where needed:** catalog search with data from other contexts and the AI Tutor's vector index.
- **No Event Sourcing.**

## Alternatives considered

- **A single model for reads and writes:** simpler, but loads full aggregates and change tracking for every read.
- **Level 3 everywhere:** eventual consistency and projection rebuilds in contexts that do not need them.
- **Event Sourcing:** full audit and temporal queries, but permanent costs in event versioning, mandatory projections, and team learning curve, with no business driver.

## Consequences

- Reads and writes are optimized independently at almost no cost.
- Read models fed by events are eventually consistent, and the UI must account for it.
- **Revisit signal:** a real audit requirement (for example payment or price history) would justify evaluating Marten for that single aggregate.
