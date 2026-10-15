# Quality attributes

Quality attributes describe *how* LearnHub must behave, not *what* it does. They drive every architecture decision: when two options conflict, the higher-ranked attribute wins. Each attribute is stated as a measurable scenario so it can be verified, not just claimed.

## Context

- LearnHub is an online course platform with an AI tutor, built as a portfolio project.
- One developer maintains it, and it evolves continuously.
- Hard budget: free tier first, at most USD 20 per month on Azure.

## Prioritized attributes

### 1. Maintainability (including testability)

The project is long-lived, maintained by one person, and changes constantly. If every change costs more than the previous one, the project stalls.

**Scenario:** adding a feature to one service (for example, course reviews) changes no code in any other service, the domain test suite runs in under 30 seconds, and the full CI pipeline is green in under 10 minutes.

**Supported by:** Clean Architecture per service, a dependency-free domain, test-first development, CI on every pull request.

### 2. Observability

LearnHub is a distributed system. A failed enrollment can originate in Identity, Catalog, messaging, or the LLM provider. Without observability, debugging is guesswork.

**Scenario:** any failed request can be traced end to end across services with a single trace ID, and its cause found from the dashboard in under 5 minutes.

**Supported by:** OpenTelemetry logs, metrics, and traces configured once in `LearnHub.ServiceDefaults`.

### 3. Cost efficiency

The USD 20 per month budget is a hard constraint. It shapes every infrastructure choice: hosting, database, messaging, and AI model.

**Scenario:** under demo load, the monthly Azure bill stays at or below USD 20, and services scale to zero when idle.

**Supported by:** consumption-based hosting, free tiers, and no always-on replicas.

### Also considered: performance

Performance matters but does not override the attributes above.

**Scenario:** with 1,000 concurrent users, course search responds in under 200 ms at the 95th percentile.

## Accepted tradeoff: high availability

LearnHub deliberately does **not** target high availability:

- Single region, no redundant replicas, no failover.
- Cold starts are accepted: after scaling to zero, the first request may take 10 to 20 seconds.

**Why:** LearnHub is a demo without paying customers. High availability requires always-on replicas and multiple regions, which directly conflicts with cost efficiency.

**Mitigation:** health checks and standard HTTP resilience (retries, timeouts, circuit breakers) keep the system responsive while it is running.

## Revisiting

Revisit this ranking when the context changes: real users, a larger budget, or more maintainers. Decisions that depend on it are recorded as ADRs.
