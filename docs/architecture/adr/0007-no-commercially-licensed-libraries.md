# 0007. No commercially licensed libraries

- Status: Accepted
- Date: 2026-10-17

## Context

MediatR, MassTransit, and AutoMapper moved to commercial licenses. LearnHub is a public portfolio project on a minimal budget and must stay free to build, fork, and run.

## Decision

We do not use MediatR, MassTransit, or AutoMapper. Wolverine handles in-process handlers and messaging; mapping is written by hand, usually as a LINQ projection. New dependencies are checked for license terms before they are added to `Directory.Packages.props`.

## Alternatives considered

- **Pin the last free versions:** no security fixes and growing upgrade debt.
- **Buy licenses:** outside the budget.
- **Mapperly for mapping:** a valid free source generator; kept as an option if hand-written mapping becomes repetitive.

## Consequences

- No licensing risk, and mapping errors surface at compile time.
- Wolverine is less widespread than MediatR, so there is less community material.
