# 0001. Record architecture decisions

- Status: Accepted
- Date: 2026-10-17

## Context

Decisions that are hard to reverse (service boundaries, persistence, communication style) get lost in chat history and commit messages. Months later nobody remembers why a choice was made, so it is either followed blindly or reverted without understanding its tradeoffs.

## Decision

We record every architecturally significant decision as an ADR in `docs/architecture/adr/`, using the template in the [index](README.md). ADRs are written in English, numbered sequentially, and never edited after acceptance except to change their status.

## Alternatives considered

- **Wiki or external document:** drifts from the code and is not reviewed in pull requests.
- **No written record:** cheapest today, most expensive when the decision is questioned.

## Consequences

- Decisions are reviewed in pull requests like code and versioned with it.
- Writing an ADR adds a small cost to each significant decision; trivial or easily reversible choices do not need one.
