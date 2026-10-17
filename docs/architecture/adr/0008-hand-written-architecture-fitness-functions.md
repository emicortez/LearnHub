# 0008. Hand-written architecture fitness functions

- Status: Accepted
- Date: 2026-10-17

## Context

Architecture rules written only in documentation erode one commit at a time. The domain rules (no external dependencies, no public constructors, no public setters) must fail the build when broken. In .NET they can be checked with plain reflection, NetArchTest, or ArchUnitNET.

## Decision

Architecture rules are enforced in `tests/LearnHub.Architecture.Tests` with plain reflection over the domain assemblies (`DomainDependencyTests`, `DomainEncapsulationTests`). Every new domain assembly is registered in both. Each new rule is seen failing on a deliberate violation before it is trusted.

## Alternatives considered

- **NetArchTest:** a fluent API for dependency rules, but it is not actively maintained and adds a package for a few rules.
- **ArchUnitNET:** more powerful (layers, cycles, slices) and maintained, but has a steeper learning curve that the current rules do not need.

## Consequences

- Zero extra dependencies and rules that read like ordinary tests.
- Complex rules such as cycle detection would need custom code.
- **Revisit signal:** when Application and Infrastructure layers exist and layer or cycle rules multiply, evaluate ArchUnitNET.
