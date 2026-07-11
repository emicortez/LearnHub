# Progress Tracker — LearnHub

> Actualizar cada semana. Marca [x] cuando completás un ítem.
> Este archivo es tu espejo de lo que sabés y lo que falta.

---

## Estado General

```
Bloque 1 — Arquitectura y Core    [ 0/4 semanas ]
Bloque 2 — Microservices Avanzado [ 0/4 semanas ]
Bloque 3 — Cloud + DevOps         [ 0/4 semanas ]
Bloque 4 — AI Integration         [ 0/4 semanas ]
Final    — Interview Prep          [ 0/2 semanas ]
```

---

## BLOQUE 1 — Arquitectura y Core

### Semana 1 — DDD + Identity Service (28 Abr – 4 May)
**Estado:** 🟡 En progreso — Día 1 completado

Conceptos:
- [x] DDD: Entities, Value Objects, Aggregates
- [x] Domain Events
- [x] Clean Architecture: las 4 capas y sus responsabilidades

Build:
- [x] Solución base con .NET Aspire
- [x] Primitivas: AggregateRoot, ValueObject, DomainEvent
- [x] Value Objects: Email, UserId
- [x] Interfaces: IUserRepository, IPasswordHasher
- [x] User aggregate (Día 1)
- [ ] Identity.Application — RegisterUser, LoginUser commands
- [ ] Identity.Infrastructure — EF Core, repositorios, BCryptPasswordHasher
- [ ] Identity.API — endpoints + JWT generation
- [ ] Refresh token flow
- [ ] Unit tests del domain

Interview prep:
- [ ] Podés explicar Clean Architecture en 2 minutos
- [ ] Sabés la diferencia entre Entity, Value Object y Aggregate
- [ ] Podés justificar por qué DDD sobre Active Record

---

### Semana 2 — CQRS + Catalog Service (5–11 May)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] CQRS: Commands vs Queries, por qué separar
- [ ] MediatR: pipeline behaviors
- [ ] Repository Pattern + Unit of Work

Build:
- [ ] Catalog.Domain — Course aggregate
- [ ] Commands: CreateCourse, UpdateCourse, PublishCourse
- [ ] Queries: GetCourses, GetCourseById, SearchCourses
- [ ] Pipeline behaviors: logging, validation, caching
- [ ] Integration tests con Testcontainers

Interview prep:
- [ ] CQRS explicado con ejemplo real
- [ ] Cuándo CQRS es overkill

---

### Semana 3 — Docker + API Gateway (12–18 May)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Docker: imágenes, contenedores, layers
- [ ] Dockerfile multi-stage
- [ ] Docker Compose
- [ ] API Gateway patterns

Build:
- [ ] Dockerfiles multi-stage para identity + catalog
- [ ] docker-compose.yml completo
- [ ] api-gateway con YARP
- [ ] JWT validation en el gateway
- [ ] Rate limiting

Interview prep:
- [ ] Explicar Docker sin jargon técnico
- [ ] Por qué un API Gateway (no ir directo a los servicios)

---

### Semana 4 — Enrollment + gRPC + Resiliencia (19–25 May)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] gRPC: Protocol Buffers, cuándo vs REST
- [ ] API versioning
- [ ] Polly: retry, circuit breaker

Build:
- [ ] enrollment-service completo
- [ ] gRPC para catalog → enrollment communication
- [ ] API versioning en catalog
- [ ] Polly configurado en las llamadas inter-servicio

Interview prep:
- [ ] gRPC vs REST tradeoffs
- [ ] Circuit breaker explicado con ejemplo

---

## BLOQUE 2 — Microservices Avanzado

### Semana 5 — Event-Driven + RabbitMQ (26 May – 1 Jun)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Messaging asíncrono
- [ ] RabbitMQ: exchanges, queues
- [ ] MassTransit
- [ ] Idempotency en consumers

Build:
- [ ] notification-service
- [ ] MassTransit en catalog + enrollment + notification
- [ ] Eventos: CoursePublished, UserEnrolled
- [ ] Dead-letter queues
- [ ] Retry con backoff exponencial

Interview prep:
- [ ] Event-driven vs request-response
- [ ] CAP theorem explicado

---

### Semana 6 — Saga Pattern (2–8 Jun)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Distributed transactions
- [ ] Saga Orchestration vs Choreography
- [ ] Compensating transactions
- [ ] Outbox Pattern

Build:
- [ ] Enrollment Saga completo
- [ ] Compensating transactions para rollback
- [ ] Outbox Pattern implementado
- [ ] Tests del saga: happy path + failure scenarios

Interview prep:
- [ ] Saga explicado a alguien sin experiencia en microservices
- [ ] Por qué no usamos transacciones distribuidas (2PC)

---

### Semana 7 — Testing Avanzado (9–15 Jun)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Testing pyramid
- [ ] Testcontainers
- [ ] Contract testing (Pact)

Build:
- [ ] Integration tests Identity Service
- [ ] Integration tests Catalog Service
- [ ] Contract tests Enrollment → Catalog
- [ ] Coverage > 80% en capas críticas

Interview prep:
- [ ] Testing pyramid: qué va en cada nivel
- [ ] Cuándo mockear y cuándo usar Testcontainers

---

### Semana 8 — Redis + Performance (16–22 Jun)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Redis como herramienta multiuso
- [ ] Cache-Aside, Write-Through, Write-Behind
- [ ] Cache invalidation

Build:
- [ ] progress-service
- [ ] Cache-Aside en Catalog
- [ ] Progress hot data en Redis
- [ ] Cache invalidation con eventos
- [ ] BenchmarkDotNet: medir mejora de performance

Interview prep:
- [ ] Cache patterns explicados con tradeoffs
- [ ] Cache stampede: qué es y cómo evitarlo

---

## BLOQUE 3 — Cloud + DevOps

### Semana 9 — Azure (23–29 Jun)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Azure core services para .NET devs
- [ ] Container Apps vs AKS
- [ ] Key Vault + App Configuration

Build:
- [ ] media-service
- [ ] Azure Blob Storage integrado
- [ ] Key Vault para secretos
- [ ] Deploy en Azure Container Apps

Interview prep:
- [ ] Azure services que todo .NET dev debe conocer
- [ ] Cómo manejar secretos en producción

---

### Semana 10 — Kubernetes (30 Jun – 6 Jul)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Kubernetes core objects
- [ ] Helm
- [ ] Horizontal scaling

Build:
- [ ] YAML manifests para todos los servicios
- [ ] Helm charts de LearnHub
- [ ] Deploy en AKS
- [ ] HPA configurado
- [ ] Dominio + TLS con cert-manager

Interview prep:
- [ ] Kubernetes en 5 minutos: por qué es necesario
- [ ] Rolling update vs recreate strategy

---

### Semana 11 — CI/CD (7–13 Jul)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] CI/CD pipeline completo
- [ ] Branching strategies
- [ ] Security scanning

Build:
- [ ] GitHub Actions workflows completos
- [ ] Security scanning con Trivy
- [ ] Environments con approvals

Interview prep:
- [ ] CI/CD best practices
- [ ] Trunk-based development vs GitFlow

---

### Semana 12 — Observabilidad (14–20 Jul)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Logs, Metrics, Traces
- [ ] Distributed tracing
- [ ] SLO/SLI/SLA

Build:
- [ ] OpenTelemetry en todos los servicios
- [ ] Grafana dashboards
- [ ] Alertas configuradas

Interview prep:
- [ ] Los tres pilares de observabilidad
- [ ] Cómo debuggear en producción sin acceso directo

---

## BLOQUE 4 — AI Integration

### Semana 13 — Semantic Kernel (21–27 Jul)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Semantic Kernel: plugins, functions, kernel
- [ ] Microsoft.Extensions.AI
- [ ] Chat completion vs embeddings

Build:
- [ ] ai-service skeleton
- [ ] Azure OpenAI integrado
- [ ] Plugin de Catalog para el LLM
- [ ] Local dev con Ollama + Phi-4

Interview prep:
- [ ] Semantic Kernel vs LangChain
- [ ] Cómo integrar AI sin lock-in de provider

---

### Semana 14 — RAG (28 Jul – 3 Ago)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] RAG pipeline completo
- [ ] Chunking strategies
- [ ] RAG vs fine-tuning

Build:
- [ ] Indexado de catálogo en Qdrant
- [ ] Pipeline de ingesta automático
- [ ] Semantic search funcional
- [ ] Q&A sobre contenido de cursos

Interview prep:
- [ ] RAG explicado en 2 minutos
- [ ] Cuándo RAG no es la solución

---

### Semana 15 — Vector DBs + Recomendaciones (4–10 Ago)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] Vector databases
- [ ] Cosine similarity
- [ ] Filtrado híbrido

Build:
- [ ] Motor de recomendaciones
- [ ] Semantic search en el frontend
- [ ] Filtros combinados

Interview prep:
- [ ] Vector DBs: cuándo usar cuál
- [ ] Similarity search explicado sin fórmulas

---

### Semana 16 — AI en Producción + Agents (11–17 Ago)
**Estado:** ⬜ No iniciado

Conceptos:
- [ ] AI Agents
- [ ] MCP Protocol
- [ ] Guardrails y content filtering
- [ ] Cost management

Build:
- [ ] Chatbot con streaming
- [ ] AI Agent completo
- [ ] MCP server básico
- [ ] Dashboard de costos

Interview prep:
- [ ] AI Agents explicados
- [ ] MCP: qué es y por qué importa
- [ ] AI en producción: los problemas reales

---

## FINAL — Interview Prep

### Semana 17 — System Design (18–24 Ago)
**Estado:** ⬜ No iniciado

- [ ] Diseñar YouTube desde cero
- [ ] Diseñar sistema de pagos
- [ ] Diseñar Uber/ride-sharing
- [ ] Presentación de LearnHub lista
- [ ] 10 behavioral questions preparadas

---

### Semana 18 — Mock Interviews (25–31 Ago)
**Estado:** ⬜ No iniciado

- [ ] Mock interview técnica completa
- [ ] Mock interview de coding
- [ ] Weak topics reforzados
- [ ] GitHub profile optimizado
- [ ] LinkedIn actualizado

---

## Skills Dominados (actualizar a medida que avanzás)

### Backend
- [ ] Clean Architecture
- [ ] DDD
- [ ] CQRS + MediatR
- [ ] Microservices
- [ ] gRPC
- [ ] Event-driven architecture
- [ ] Saga Pattern
- [ ] Outbox Pattern

### Cloud & DevOps
- [ ] Docker
- [ ] Kubernetes
- [ ] Azure Container Apps
- [ ] AKS
- [ ] GitHub Actions CI/CD
- [ ] OpenTelemetry
- [ ] Bicep/Terraform

### Frontend
- [ ] React 19 (avanzado)
- [ ] Next.js 15 (App Router)
- [ ] TanStack Query
- [ ] TypeScript (avanzado)

### AI
- [ ] Semantic Kernel
- [ ] RAG
- [ ] Vector Databases
- [ ] AI Agents
- [ ] MCP Protocol
- [ ] Azure OpenAI

### Databases
- [ ] PostgreSQL (avanzado)
- [ ] Redis (avanzado)
- [ ] Qdrant

### System Design
- [ ] Escalabilidad horizontal vs vertical
- [ ] CAP theorem
- [ ] Distributed systems patterns
- [ ] Database sharding
- [ ] Event sourcing
