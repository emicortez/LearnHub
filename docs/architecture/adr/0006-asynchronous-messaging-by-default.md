# 0006. Asynchronous messaging between services by default

- Status: Accepted
- Date: 2026-10-17

## Context

Synchronous call chains between services multiply unavailability (three dependencies at 99.9% leave the caller at about 99.7%) and couple services in time. With microservices ([ADR 0002](0002-microservices-with-a-database-per-service.md)), this is the main path to a distributed monolith.

## Decision

- **Between services:** integration events published with Wolverine by default (for example `CoursePublished`, `StudentEnrolled`). Consumers keep a local copy of the data they need.
- **Frontend to services:** REST with Minimal APIs and OpenAPI.
- **Authentication:** each service validates JWTs locally with cached public keys; no service calls Identity per request.
- **Synchronous service-to-service calls:** only as an exception when an immediate answer is required, always with the timeouts, retries, and circuit breaker from `AddServiceDefaults`.
- **gRPC:** not used until a low-latency internal call justifies it.

## Alternatives considered

- **Synchronous HTTP between services:** simpler to debug, but failures cascade.
- **gRPC between services:** faster and strongly typed, but there is no latency-sensitive internal call today.

## Consequences

- Failures stay isolated: an unavailable consumer processes its messages when it recovers.
- Consistency between services is eventual, so consumers must be idempotent, and reliable publishing needs an outbox.
- Debugging requires distributed tracing, which is already in place through OpenTelemetry.
