# Plan Maestro — LearnHub

> 18 semanas. Empezamos: 28 abril 2026. Target: entrevistas en septiembre 2026.

---

## Resumen por Bloque

| Bloque | Semanas | Foco | Objetivo |
|--------|---------|------|---------|
| **Bloque 1** | 1–4 | Arquitectura y Core | Salir del CRUD monolítico |
| **Bloque 2** | 5–8 | Microservices Avanzado | Sistemas distribuidos reales |
| **Bloque 3** | 9–12 | Cloud + DevOps | Deploy real, CI/CD, observabilidad |
| **Bloque 4** | 13–16 | AI Integration | Features de IA en producción |
| **Final** | 17–18 | Interview Prep | Estar listo para cualquier entrevista |

---

## Cronograma Diario

```
LUNES A VIERNES (1 hs):
  ⚡  60 min  → Código directo en LearnHub con explicación integrada
               (La teoría se aprende haciendo, no antes de hacer)

SÁBADO (opcional, 1-2 hs):
  🔨  Feature compleja, refactor, o tema pendiente de la semana

DOMINGO (opcional, 30-60 min):
  🧠  System design / prep entrevistas / revisar semana
```

> Con 1h/día los conceptos se aprenden EN el código, no antes.
> Cada archivo tiene comentarios que explican el POR QUÉ, no el qué.

---

## BLOQUE 1 — Arquitectura y Core (Semanas 1–4)

### Semana 1 — 28 Abr al 4 May
**Tema: DDD + Clean Architecture → Identity Service**

**Conceptos:**
- Modular Monolith vs Microservices — cuándo cada uno y por qué empezamos con microservices en LearnHub como ejercicio deliberado
- Domain-Driven Design: Entities, Value Objects, Aggregates
- Domain Events — qué son, cuándo dispararlos
- Clean Architecture en la práctica (no en teoría)
- Result Pattern con ErrorOr — `ErrorOr<T>` en lugar de exceptions para errores esperados
- Architecture Fitness Functions — tests que protegen la Dependency Rule
- OAuth2 — qué es y cuándo cada flow: Authorization Code + PKCE (usuarios desde el browser), Client Credentials (service-to-service sin usuario)
- OpenID Connect (OIDC) — la capa de identidad sobre OAuth2: la diferencia entre "qué podés hacer" (OAuth2) y "quién sos" (OIDC)
- Autenticación vs Autorización — conceptual pero fundamental, muchos lo confunden en entrevistas

**Build en LearnHub:**
- [ ] Crear solución base con .NET Aspire
- [ ] `identity-service` — Clean Architecture completa
- [ ] User registration con dominio (no anemic models)
- [ ] ErrorOr en los handlers — handlers retornan `ErrorOr<T>`, no lanzan exceptions de negocio
- [ ] JWT access token (15min) + Refresh token (7d)
- [ ] OAuth2 Authorization Code + PKCE flow — identity-service como Authorization Server básico
- [ ] Client Credentials flow — para que los servicios se autentiquen entre sí sin usuario
- [ ] Domain Events: `UserRegistered`
- [ ] Unit tests para el domain
- [ ] ArchUnitNET: test que falla si Domain importa algo de Infrastructure

**Interview topics cubiertos:**
- Qué es Clean Architecture y por qué importa
- Modular Monolith vs Microservices — tradeoffs reales
- SOLID en la práctica
- Diferencia entre Entity y Value Object
- Por qué DDD y no Active Record
- Result pattern vs exceptions — cuándo cada uno
- OAuth2 vs JWT — no son lo mismo: JWT es un formato de token, OAuth2 es un protocolo de autorización
- Authorization Code + PKCE vs Client Credentials — cuándo cada flow y por qué
- OpenID Connect — por qué existe si ya tenemos OAuth2

**Daily breakdown:**
- Lun: Setup del repo y solución Aspire. Concepto: Modular Monolith vs Microservices.
- Mar: Identity.Domain — User entity, Email/Password value objects, ErrorOr.
- Mié: Application layer — RegisterUser command + handler con ErrorOr.
- Jue: Infrastructure — EF Core, UserRepository, migraciones.
- Vie: API layer — endpoint register/login. JWT. OAuth2 flows (Auth Code + PKCE, Client Credentials). Scalar para OpenAPI.
- Sáb: Refresh tokens + ArchUnitNET tests de arquitectura.
- Dom: System design: diseñar un sistema de autenticación desde cero.

---

### Semana 2 — 5 al 11 May
**Tema: CQRS + MediatR → Catalog Service**

**Conceptos:**
- CQRS — Commands (escriben, via EF Core) vs Queries (leen, via Dapper)
- Por qué el read side no necesita EF Core — Dapper es 3-10x más rápido para queries
- MediatR — pipeline, behaviors
- Repository Pattern + Unit of Work (solo para el write side)
- Specification Pattern para queries complejas reutilizables

**Build en LearnHub:**
- [ ] `catalog-service` — Clean Architecture
- [ ] Course aggregate con toda la lógica de dominio
- [ ] CreateCourse, UpdateCourse, PublishCourse commands via MediatR + EF Core
- [ ] GetCourses, GetCourseById queries — Dapper directo, sin repositorio, sin EF
- [ ] Pipeline behaviors: logging, validation (FluentValidation), performance timing
- [ ] Integration tests con Testcontainers
- [ ] Scalar configurado para OpenAPI docs del servicio

**Interview topics cubiertos:**
- CQRS vs MVC tradicional
- Por qué EF Core en writes y Dapper en reads — el argumento de performance
- Cuándo CQRS tiene sentido (y cuándo no)
- MediatR y el patrón Mediator
- Specification Pattern

---

### Semana 3 — 12 al 18 May
**Tema: Docker + API Gateway + BFF**

**Conceptos:**
- Contenedores: por qué existen, diferencia con VMs
- Dockerfile multi-stage build + Native AOT para imágenes mínimas
- Docker Compose para orquestación local
- API Gateway Pattern: routing, rate limiting, auth centralizada
- Backend for Frontend (BFF) — gateway especializado por cliente, evita over-fetching
- Dapr introducción: el sidecar que abstrae infraestructura distribuida

**Build en LearnHub:**
- [ ] Dockerfile optimizado para cada servicio (multi-stage)
- [ ] `docker-compose.yml` completo: servicios + PostgreSQL + Redis + RabbitMQ + Dapr sidecars
- [ ] `api-gateway` con YARP — routing + JWT validation centralizada
- [ ] BFF para el frontend Next.js — agrega y transforma respuestas, expone lo que la UI necesita
- [ ] Rate limiting en el gateway (Redis como backend)
- [ ] Dapr service invocation básico: enrollment-service llama a catalog via Dapr

**Interview topics cubiertos:**
- Docker: imágenes, contenedores, layers, volumes, multi-stage builds
- API Gateway vs BFF — cuándo cada uno, cuándo los dos juntos
- Dapr: qué problema resuelve, cómo funciona el sidecar
- Diferencia entre Docker Compose y Kubernetes

---

### Semana 4 — 19 al 25 May
**Tema: Enrollment + gRPC + GraphQL + Resiliencia**

**Conceptos:**
- Comunicación síncrona entre servicios: REST vs gRPC vs GraphQL
- gRPC — contrato tipado via .proto, performance binario, ideal para inter-service
- GraphQL con Hot Chocolate — cuando el BFF necesita queries flexibles
- API versioning strategies (URL vs header vs media type)
- Resilience patterns con Polly v8 — retry, circuit breaker, bulkhead, timeout en pipeline

**Build en LearnHub:**
- [ ] `enrollment-service` — Clean Architecture completa
- [ ] Comprar curso — valida con Catalog via gRPC (.proto contract)
- [ ] GraphQL endpoint en el BFF con Hot Chocolate — el frontend consulta lo que necesita
- [ ] Historial de compras y progreso
- [ ] API versioning con URL path (`/api/v1/`, `/api/v2/`)
- [ ] Polly: retry + circuit breaker + timeout como pipeline en todos los HTTP clients

**Interview topics cubiertos:**
- gRPC vs REST vs GraphQL — cuándo cada uno con tradeoffs reales
- GraphQL: N+1 problem y DataLoader como solución
- Polly v8 pipeline — cómo se encadenan las estrategias de resiliencia
- API versioning strategies

---

## BLOQUE 2 — Microservices Avanzado (Semanas 5–8)

### Semana 5 — 26 May al 1 Jun
**Tema: Event-Driven Architecture + Dapr Pub/Sub**

**Conceptos:**
- Messaging asíncrono — por qué, cuándo, y cuándo NO
- RabbitMQ: exchanges, queues, bindings, routing keys
- MassTransit como abstracción sobre RabbitMQ/Azure Service Bus
- Dapr pub/sub: el mismo código, cualquier broker (RabbitMQ local → Azure Service Bus en prod)
- Outbox Pattern + Inbox Pattern — entrega garantizada e idempotencia
- CAP theorem aplicado: qué significa "eventual consistency" en la práctica

**Build en LearnHub:**
- [ ] `notification-service`
- [ ] MassTransit configurado en catalog + enrollment + notification
- [ ] Dapr pub/sub como capa adicional: enrollment publica via Dapr, notification consume
- [ ] Outbox Pattern en Enrollment Service (evento + datos en misma transacción)
- [ ] Inbox Pattern en Notification Service (procesar cada evento exactamente una vez)
- [ ] Evento `CoursePublished` → trigger indexación en AI Service
- [ ] Evento `UserEnrolled` → email de bienvenida via SendGrid
- [ ] Dead-letter queue para mensajes fallidos con retry exponencial

**Interview topics cubiertos:**
- Event-driven architecture — tradeoffs reales
- CAP theorem — Consistency, Availability, Partition Tolerance
- Outbox vs Inbox — garantía de entrega vs idempotencia
- Dapr pub/sub vs MassTransit — cuándo cada abstracción
- Idempotency: cómo garantizarla en consumers

---

### Semana 6 — 2 al 8 Jun
**Tema: Saga Pattern + Event Sourcing con Marten**

**Conceptos:**
- El problema de las transacciones distribuidas
- Saga: Orchestration vs Choreography — cuándo cada uno
- Compensating transactions (rollback distribuido)
- Event Sourcing — el estado como stream de eventos, no como fila en la DB
- Marten: event store + document DB sobre PostgreSQL (sin nueva infraestructura)
- Cuándo Event Sourcing tiene sentido (auditoría, replay, temporal queries) y cuándo no

**Build en LearnHub:**
- [ ] Enrollment Saga completo con MassTransit (Orchestration)
- [ ] Flujo: `ValidatePayment` → `EnrollUser` → `GrantAccess` → `SendNotification`
- [ ] Compensación: si falla `EnrollUser` → `CancelPayment`
- [ ] Event Sourcing en `progress-service` con Marten — el progreso del alumno como stream de eventos
- [ ] Projection en Marten: rebuild del read model de progreso desde los eventos
- [ ] Tests del flujo completo: happy path + failure scenarios + saga compensation

**Interview topics cubiertos:**
- Distributed transactions — por qué no hay rollback global
- Saga vs 2PC (Two-Phase Commit)
- Event Sourcing — cuándo sí (auditoría, replay) y cuándo no (simple CRUD)
- Marten projections — cómo rebuildeás un read model desde eventos
- Eventual consistency aplicada

---

### Semana 7 — 9 al 15 Jun
**Tema: Testing Avanzado + Architecture Fitness Functions**

**Conceptos:**
- Testing pyramid para sistemas distribuidos
- Integration tests con Testcontainers — tests que hablan con la DB real
- Contract testing entre microservicios con Pact
- Architecture Fitness Functions con ArchUnitNET — la arquitectura como test ejecutable
- Mutation testing con Stryker.NET — la diferencia entre coverage y calidad real
- Load testing con k6 — medir comportamiento bajo carga antes de ir a prod

**Build en LearnHub:**
- [ ] Integration tests completos para Identity Service con Testcontainers
- [ ] Integration tests completos para Catalog Service
- [ ] Integration tests para el Enrollment Saga (happy path + compensación)
- [ ] Consumer-driven contract tests (Pact) entre Enrollment → Catalog
- [ ] ArchUnitNET: suite completa de fitness functions para todos los servicios
- [ ] Stryker en Identity.Domain — mutation score objetivo: >80%
- [ ] k6: script de carga para el endpoint de registro + publicar cursos
- [ ] Coverage reports integrados en el pipeline

**Interview topics cubiertos:**
- Testing pyramid — unit vs integration vs E2E en microservicios
- ArchUnitNET: cómo los fitness functions protegen la arquitectura en CI
- Mutation testing — por qué el 80% de coverage puede ser mentira
- Contract testing — por qué importa cuando tenés múltiples equipos
- Load testing: métricas que importan (p50, p95, p99) y cómo leerlas

---

### Semana 8 — 16 al 22 Jun
**Tema: Caching + Performance + Real-Time**

**Conceptos:**
- Redis como herramienta multiuso: caché, pub/sub, rate limiting, sessions, leaderboards
- Cache patterns: Cache-Aside, Write-Through, Write-Behind
- Cache invalidation — el problema más difícil del software
- Cache stampede — qué es, cómo prevenirlo con mutex lock o probabilistic early expiration
- SignalR para real-time: WebSockets abstracted, scale-out con Redis backplane
- BenchmarkDotNet + k6 combinados: micro-benchmarks + load tests macro

**Build en LearnHub:**
- [ ] Cache-Aside en Catalog para GetCourses (hot path) con Dapper + Redis
- [ ] Invalidar cache de curso cuando se publica — evento → invalida
- [ ] Redis pub/sub para notificaciones en tiempo real
- [ ] SignalR hub en progress-service — progreso del alumno en tiempo real
- [ ] SignalR scale-out con Redis backplane (múltiples instancias)
- [ ] BenchmarkDotNet: comparar EF Core vs Dapper en GetCourses
- [ ] k6: load test del hot path con y sin cache — comparar p95
- [ ] Rate limiting en el API Gateway con Redis (token bucket)

**Interview topics cubiertos:**
- Cache patterns y tradeoffs con ejemplos concretos
- Cache stampede y las dos estrategias para evitarlo
- SignalR — WebSockets, long polling, SSE — cuándo cada transporte
- Redis como backplane para scale-out de SignalR
- Métricas de performance: p50 vs p95 vs p99 — qué significa cada una

---

## BLOQUE 3 — Cloud + DevOps (Semanas 9–12)

### Semana 9 — 23 al 29 Jun
**Tema: Azure para .NET Developers**

**Conceptos:**
- Azure: servicios core que todo .NET dev debe conocer
- IaaS vs PaaS vs SaaS — qué usar para qué
- Azure Container Apps: serverless containers (scale to zero)
- Azure Key Vault + App Configuration

**Build en LearnHub:**
- [ ] `media-service` — upload y streaming de videos
- [ ] Azure Blob Storage para binarios
- [ ] Azure Key Vault para connection strings y API keys
- [ ] Azure App Configuration centralizado para todos los servicios
- [ ] Deploy de los primeros 2 servicios en Azure Container Apps

**Interview topics cubiertos:**
- Azure core services para backend .NET
- Por qué Container Apps vs AKS (serverless vs managed K8s)
- Secret management en cloud

---

### Semana 10 — 30 Jun al 6 Jul
**Tema: Kubernetes + GitOps**

**Conceptos:**
- Kubernetes: por qué Docker Compose no alcanza en prod
- Pods, Deployments, Services, Ingress
- ConfigMaps, Secrets, Persistent Volumes
- Horizontal Pod Autoscaler
- GitOps con ArgoCD — el estado del cluster vive en Git, no en tu mente
- Rolling update vs Blue-Green vs Canary en Kubernetes real

**Build en LearnHub:**
- [ ] Manifests YAML para todos los servicios
- [ ] Helm charts para LearnHub
- [ ] Deploy en AKS (Azure Kubernetes Service)
- [ ] Ingress con NGINX + cert-manager (TLS automático)
- [ ] HPA: escalar catalog-service bajo carga con k6
- [ ] ArgoCD: repo de infra en Git, ArgoCD aplica los cambios automáticamente
- [ ] Blue-Green deployment de identity-service con zero downtime
- [ ] kubectl basics: logs, exec, port-forward, describe

**Interview topics cubiertos:**
- Por qué Kubernetes — cuándo vale el overhead
- Rolling updates y zero-downtime deployments
- GitOps: qué es, por qué Git como source of truth para infraestructura
- ArgoCD: cómo funciona el reconciliation loop
- Kubernetes vs Container Apps — cuándo cada uno

---

### Semana 11 — 7 al 13 Jul
**Tema: CI/CD con GitHub Actions + Security**

**Conceptos:**
- CI/CD: Continuous Integration y Continuous Deployment
- Branching strategies: trunk-based vs GitFlow
- Blue-green, canary, rolling deployments
- Feature flags como desacoplamiento de deployment y release
- Security en el pipeline (Shift-Left Security)

**Build en LearnHub:**
- [ ] Pipeline completo para cada microservicio
- [ ] Build → Lint → Test → Docker build → Push to ACR → Deploy
- [ ] Environments: dev, staging, production con approval gates
- [ ] Security scanning (Trivy + OWASP ZAP) en el pipeline
- [ ] Secrets en GitHub Actions — Key Vault integrado, nunca hardcodeados
- [ ] Feature flag básico con Azure App Configuration
- [ ] GitOps: estado del cluster en Git

**Interview topics cubiertos:**
- CI/CD pipeline best practices y shift-left security
- Trunk-based development vs GitFlow
- Blue-green vs canary vs rolling deployments
- Feature flags — deployment vs release

---

### Semana 12 — 14 al 20 Jul
**Tema: Observabilidad**

**Conceptos:**
- Los tres pilares: Logs, Metrics, Traces
- Distributed tracing — seguir una request a través de múltiples servicios
- Health checks y readiness/liveness probes
- SLO, SLI, SLA — qué significan

**Build en LearnHub:**
- [ ] OpenTelemetry en todos los servicios
- [ ] Distributed tracing: una compra visible end-to-end
- [ ] Grafana dashboards: latencia, error rate, throughput
- [ ] Structured logging con Serilog + Seq
- [ ] Health checks en cada servicio + endpoints `/health` y `/ready`
- [ ] Alertas básicas: error rate > 1%, latencia > 500ms

**Interview topics cubiertos:**
- Qué es observabilidad (no solo logging)
- Por qué OpenTelemetry se convirtió en estándar
- Incident response: cómo debuggear en producción

---

## BLOQUE 4 — AI Integration (Semanas 13–16)

### Semana 13 — 21 al 27 Jul
**Tema: Microsoft AgentFramework + LLM Integration**

**Conceptos:**
- Microsoft AgentFramework — arquitectura de agentes en .NET
- Microsoft.Extensions.AI como abstracción de provider (Azure OpenAI → Ollama)
- Tool Use / Function Calling — el LLM decide qué herramienta usar
- Diferencia entre chat completion, embeddings, y fine-tuning
- Prompt Engineering — few-shot, zero-shot, chain-of-thought

**Build en LearnHub:**
- [ ] `ai-service` skeleton con Microsoft AgentFramework
- [ ] Microsoft.Extensions.AI configurado — Azure OpenAI + Ollama para dev
- [ ] Tool de Catalog: el agente puede buscar cursos via function calling
- [ ] Chat completion API expuesta via REST con streaming (SSE)
- [ ] Configuración local con Ollama (Phi-4) para desarrollo sin costos
- [ ] Semantic Cache básico para no repetir llamadas idénticas

**Interview topics cubiertos:**
- Microsoft AgentFramework vs Semantic Kernel — diferencias y evolución
- Cuándo usar AI en una aplicación real (y cuándo NO)
- Tokens, costos, rate limiting de APIs de AI
- Tool Use — cómo el LLM decide qué función llamar

---

### Semana 14 — 28 Jul al 3 Ago
**Tema: RAG — Retrieval-Augmented Generation**

**Conceptos:**
- El problema que RAG resuelve (LLM sin contexto propio)
- Pipeline RAG: ingest → embed → store → retrieve → generate
- Chunking strategies
- RAG vs fine-tuning — cuándo cada uno

**Build en LearnHub:**
- [ ] Indexar todo el catálogo de cursos como embeddings en Qdrant
- [ ] Pipeline de ingesta: curso publicado → chunking → embeddings → Qdrant
- [ ] Search inteligente: "quiero aprender microservicios en .NET" → cursos relevantes
- [ ] Q&A sobre el contenido de un curso específico
- [ ] Reranking básico para mejores resultados

**Interview topics cubiertos:**
- Qué es RAG y cómo funciona
- Embeddings: qué son conceptualmente
- Chunking strategies y por qué importan
- Limitaciones del RAG

---

### Semana 15 — 4 al 10 Ago
**Tema: Vector Databases + Semantic Search**

**Conceptos:**
- Vector databases vs DBs relacionales
- Cosine similarity, dot product
- Índices HNSW
- Filtrado con metadata + búsqueda vectorial combinados

**Build en LearnHub:**
- [ ] Motor de recomendaciones: "si te gustó este curso, te pueden gustar estos"
- [ ] Semantic search en el frontend (search bar con NLP)
- [ ] Filtros combinados: categoría + nivel + relevancia semántica
- [ ] Cache de embeddings para no recomputar
- [ ] Actualización incremental del índice cuando se publica un curso nuevo

**Interview topics cubiertos:**
- Vector DBs: cuándo usar Qdrant vs pgvector vs Azure AI Search
- Similarity search explicado sin matemáticas
- Casos de uso reales

---

### Semana 16 — 11 al 17 Ago
**Tema: AI en Producción + Multi-Agent Workflows**

**Conceptos:**
- Multi-Agent Workflows — agentes especializados que colaboran
- ReAct loop (Reason + Act) — cómo un agente razona y actúa
- MCP (Model Context Protocol) — conectar agentes con herramientas externas
- Guardrails, content filtering, prompt injection defense
- Cost management: Model Router (Haiku para simple, GPT-4o para complejo)
- Observabilidad de AI: traces de agentes, tokens, latencia, costos

**Build en LearnHub:**
- [ ] Multi-agent workflow: Agente de Soporte orquesta a Agente de Catalog y Agente de Enrollment
- [ ] Chatbot con streaming SSE y historial de conversación
- [ ] MCP server de LearnHub — expone tools al ecosistema de agentes
- [ ] Model Router: despacha al modelo correcto según complejidad del request
- [ ] Content filtering con Azure AI Content Safety
- [ ] Dashboard de costos AI: tokens usados, costo estimado por feature
- [ ] Rate limiting con Redis específico para endpoints de AI
- [ ] Observabilidad de agentes con OpenTelemetry (traces de cada tool call)

**Interview topics cubiertos:**
- Multi-Agent Workflows: orquestación vs choreography en agentes
- ReAct pattern — cómo funciona el loop de un agente
- MCP Protocol — por qué se convirtió en estándar
- AI en producción: costos, latencia, failure modes
- Responsible AI: guardrails, content filtering, prompt injection

---

## FINAL — Interview Prep (Semanas 17–18)

### Semana 17 — 18 al 24 Ago
**Tema: System Design + Portfolio**

- [ ] Practicar diseño de sistemas: YouTube, Uber, Booking, WhatsApp
- [ ] Preparar presentación de LearnHub: cada decisión justificada
- [ ] Behavioral questions: STAR method, situaciones reales
- [ ] LinkedIn optimizado con LearnHub como proyecto destacado

**Daily:**
- Lun: System design — diseñar Netflix desde cero
- Mar: System design — diseñar un sistema de pagos
- Mié: Revisar y documentar todas las decisiones de arquitectura de LearnHub
- Jue: Behavioral questions — preparar 10 situaciones con STAR
- Vie: Grabar o simular presentación de LearnHub (5 minutos)
- Sáb: Mock interview completa (sistema + código + behavioral)
- Dom: Identificar gaps y reforzar

---

### Semana 18 — 25 al 31 Ago
**Tema: Mock Interviews + Polish**

- [ ] Mock interview técnica completa (arquitectura)
- [ ] Mock interview de coding (algoritmos básicos)
- [ ] Revisar weak topics identificados
- [ ] GitHub profile: README, repos organizados, LearnHub destacado
- [ ] Preparar 5 historias de tu experiencia en startup (con métricas reales)

---

## Checklist Pre-Entrevista

### Proyecto
- [ ] LearnHub deployado y funcionando en la nube (Azure Container Apps + AKS)
- [ ] README explica la arquitectura con diagrama y decisiones justificadas
- [ ] Podés explicar CADA decisión técnica y sus tradeoffs
- [ ] Conocés los límites de cada tecnología que usaste
- [ ] GitHub profile con LearnHub destacado, commits con Conventional Commits, PRs documentados

### Arquitectura
- [ ] Clean Architecture + DDD: explicar en 3 minutos con ejemplos reales de LearnHub
- [ ] Modular Monolith vs Microservices: cuándo cada uno, cuál hubieras elegido en un startup
- [ ] CQRS: por qué EF Core en writes y Dapper en reads — el argumento técnico
- [ ] Event Sourcing con Marten: cuándo tiene sentido, cuándo es overkill
- [ ] Saga Orchestration vs Choreography: tradeoffs con ejemplo del Enrollment Saga
- [ ] Outbox + Inbox: garantía de entrega sin 2PC

### Distributed Systems
- [ ] CAP theorem — dar un ejemplo concreto de cada combinación CP/AP/CA
- [ ] Eventual consistency — qué significa para el usuario final
- [ ] Idempotency — cómo la implementaste en los consumers
- [ ] Distributed tracing — poder seguir una request de registro end-to-end en Grafana

### Performance & Reliability
- [ ] Cache stampede — explicar qué es y cómo lo preveniste
- [ ] Circuit breaker — cuándo se abre, cuándo se cierra, estado half-open
- [ ] p50 vs p95 vs p99 — qué miden y cuál le importa al usuario
- [ ] Load testing con k6 — resultados reales de LearnHub bajo carga

### Testing
- [ ] Testing pyramid con ejemplos concretos de cada nivel en LearnHub
- [ ] ArchUnitNET: qué fitness functions escribiste y por qué
- [ ] Mutation testing: cuál fue el mutation score de Identity.Domain
- [ ] Contract testing con Pact: qué protege y qué no protege

### DevOps & Cloud
- [ ] CI/CD pipeline completo con shift-left security — explicar cada paso
- [ ] GitOps con ArgoCD — cómo funciona el reconciliation loop
- [ ] Blue-Green deployment — cómo el rollback toma segundos
- [ ] Azure Container Apps vs AKS — cuándo cada uno con costos reales

### AI
- [ ] Microsoft AgentFramework: arquitectura de un agente, tool use, ReAct loop
- [ ] RAG pipeline completo: ingest → embed → retrieve → generate
- [ ] Model Router: cuándo Phi-4 local, cuándo GPT-4o — con el argumento de costos
- [ ] MCP: qué es, cómo lo implementaste en el MCP server de LearnHub

### Soft Skills
- [ ] Tenés ejemplos concretos de situaciones laborales (STAR method) — mínimo 10
- [ ] Podés explicar LearnHub como tu propio producto: qué resuelve, para quién, por qué las decisiones
- [ ] Podés hablar de tradeoffs sin defender una tecnología a muerte
- [ ] System design: podés diseñar YouTube, Uber, Booking, y WhatsApp sin ayuda
