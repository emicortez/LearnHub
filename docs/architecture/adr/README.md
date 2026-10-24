# Architecture Decision Records

An ADR records one architecturally significant decision: its context, the choice, and its consequences. ADRs are immutable once accepted; to change a decision, write a new ADR that supersedes the old one and update the old one's status.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-microservices-with-a-database-per-service.md) | Microservices with a database per service | Accepted |
| [0003](0003-clean-architecture-inside-each-service.md) | Clean Architecture inside each service | Accepted |
| [0004](0004-repository-per-aggregate-for-writes.md) | Repository per aggregate for writes, DbContext for reads | Accepted |
| [0005](0005-cqrs-level-2-without-event-sourcing.md) | CQRS level 2 without Event Sourcing | Accepted |
| [0006](0006-asynchronous-messaging-by-default.md) | Asynchronous messaging between services by default | Accepted |
| [0007](0007-no-commercially-licensed-libraries.md) | No commercially licensed libraries | Accepted |
| [0008](0008-hand-written-architecture-fitness-functions.md) | Hand-written architecture fitness functions | Accepted |
| [0009](0009-delegated-authentication-with-entra-external-id.md) | Delegated authentication with Microsoft Entra External ID | Accepted |
| [0010](0010-browser-sessions-through-a-backend-for-frontend.md) | Browser sessions through a Backend for Frontend | Accepted |

## Template

```markdown
# NNNN. Title in the imperative

- Status: Proposed | Accepted | Superseded by NNNN
- Date: YYYY-MM-DD

## Context
The forces at play: quality attributes, constraints, and the problem to solve.

## Decision
What we do, stated in active voice.

## Alternatives considered
Each option with the reason it was not chosen.

## Consequences
What becomes easier, what becomes harder, and the signal that would make us revisit this decision.
```
