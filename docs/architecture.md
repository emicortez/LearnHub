# Arquitectura — LearnHub

> Diseño del sistema completo. Se actualiza a medida que se construye.

---

## Visión General

LearnHub es una plataforma de aprendizaje online construida con **arquitectura de microservicios**. Cada servicio es independiente: tiene su propia base de datos, se despliega por separado, y se comunica via API o mensajes.

---

## Diagrama de Servicios

```
                        ┌─────────────────────┐
                        │   INTERNET / USERS  │
                        └──────────┬──────────┘
                                   │
                        ┌──────────▼──────────┐
                        │   API GATEWAY (YARP) │
                        │   learnhub.com/api   │
                        └──┬───┬───┬───┬───┬──┘
                           │   │   │   │   │
              ┌────────────┘   │   │   │   └───────────────┐
              │            ┌───┘   └───┐                    │
              ▼            ▼           ▼                    ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐      ┌──────────┐
        │ Identity │ │ Catalog  │ │Enrollment│      │   AI     │
        │ Service  │ │ Service  │ │ Service  │      │ Service  │
        └────┬─────┘ └────┬─────┘ └────┬─────┘      └────┬─────┘
             │            │            │                   │
            PG           PG           PG                 Qdrant
                                                        Azure OAI

              ┌──────────┐ ┌──────────┐ ┌──────────────┐
              │  Media   │ │ Progress │ │ Notification │
              │ Service  │ │ Service  │ │   Service    │
              └────┬─────┘ └────┬─────┘ └──────┬───────┘
                   │            │              │
              Azure Blob    PG + Redis    PG + SignalR

                        ┌─────────────────────┐
                        │     RabbitMQ /       │
                        │  Azure Service Bus   │
                        │  (Event Bus)         │
                        └─────────────────────┘
```

---

## Servicios

### Identity Service
- **Responsabilidad:** Autenticación, usuarios, roles, permisos
- **Tecnologías:** .NET 10, ASP.NET Core, EF Core, OpenIddict, BCrypt
- **DB:** PostgreSQL (users, roles, permissions, refresh_tokens)
- **Expone:** OAuth2/OIDC endpoints + User management API
- **Emite eventos:** `UserRegistered`, `UserDeleted`

### Catalog Service
- **Responsabilidad:** Cursos, categorías, instructores, reviews
- **Tecnologías:** .NET 10, MediatR (CQRS), EF Core
- **DB:** PostgreSQL
- **Expone:** REST API + gRPC interno
- **Emite eventos:** `CoursePublished`, `CourseUpdated`, `CourseDeleted`
- **Consume:** `UserDeleted` (para anonimizar instructor)

### Media Service
- **Responsabilidad:** Upload y streaming de videos, thumbnails
- **Tecnologías:** .NET 10, Azure Blob Storage, background processing
- **DB:** PostgreSQL (metadata) + Azure Blob (binarios)
- **Emite eventos:** `VideoProcessed`, `VideoDeleted`

### Enrollment Service
- **Responsabilidad:** Compras, suscripciones, historial de pagos
- **Tecnologías:** .NET 10, Stripe SDK, Saga Pattern
- **DB:** PostgreSQL
- **Emite eventos:** `UserEnrolled`, `PaymentProcessed`, `EnrollmentCancelled`
- **Consume:** `CourseDeleted` (para manejar cursos comprados)

### Progress Service
- **Responsabilidad:** Tracking de avance por lección, certificados, badges
- **Tecnologías:** .NET 10, Redis (hot path), EF Core
- **DB:** PostgreSQL + Redis
- **Emite eventos:** `LessonCompleted`, `CourseCompleted`, `CertificateGenerated`
- **Consume:** `UserEnrolled` (activar tracking)

### Notification Service
- **Responsabilidad:** Emails transaccionales, notificaciones en app, real-time
- **Tecnologías:** .NET 10, SignalR, SendGrid
- **DB:** PostgreSQL (historial)
- **No emite eventos** — es consumidor puro
- **Consume:** Casi todos los eventos del sistema

### AI Service
- **Responsabilidad:** Recomendaciones, búsqueda semántica, chatbot RAG, summaries
- **Tecnologías:** .NET 10, Semantic Kernel 1.x, Microsoft.Extensions.AI, Azure OpenAI, Qdrant
- **DB:** Qdrant (vectors) + Redis (cache de AI responses)
- **Integración:** Lee de Catalog para indexar contenido, expone API de AI features

### API Gateway (YARP)
- **Responsabilidad:** Entry point único, routing, rate limiting, auth validation
- **Tecnologías:** .NET 10, YARP 2.x
- **Valida JWT** antes de hacer forward al servicio
- **Rate limiting** por usuario y por endpoint

---

## Frontend

```
Next.js 15 App (React 19 + TypeScript)
├── App Router (layouts, loading, error boundaries)
├── Server Components (fetch de datos en servidor)
├── Client Components (interactividad, state)
├── TanStack Query (server state, cache, mutations)
├── Zustand (client state — user, cart, UI)
└── Tailwind 4 + Shadcn/ui (componentes)
```

El frontend habla **solo con el API Gateway**. Nunca directamente con un microservicio.

---

## Comunicación entre Servicios

### Síncrona (request/response)
- **HTTP/REST:** Comunicación external (frontend → gateway → servicio)
- **gRPC:** Comunicación internal performance-critical (Catalog → Progress para validar enrollment)

### Asíncrona (fire and forget / event-driven)
- **RabbitMQ / Azure Service Bus** via **MassTransit**
- Usado cuando un servicio no necesita esperar la respuesta

### Cuándo usar cada uno

| Situación | Usar |
|-----------|------|
| El servicio A necesita datos del B para responder | HTTP/gRPC |
| El servicio A notifica al B de algo que pasó | Evento async |
| Flujo multi-servicio (compra → inscripción → notificación) | Saga + eventos |

---

## Patrón de Arquitectura Interna (por servicio)

Cada microservicio sigue **Clean Architecture**:

```
ServiceName/
├── Domain/              # Entities, Value Objects, Domain Events, Interfaces
├── Application/         # CQRS Commands/Queries, Handlers, DTOs, Validators
├── Infrastructure/      # EF Core, repositorios, servicios externos
└── API/                 # Controllers/Minimal APIs, Middleware, DI setup
```

---

## Infraestructura

### Local (desarrollo)
```
docker-compose.yml
├── Todos los microservicios
├── PostgreSQL (una instancia por servicio, diferentes DBs)
├── Redis
├── RabbitMQ
├── Qdrant
├── Seq (logs)
└── .NET Aspire Dashboard (métricas, traces)
```

### Cloud (Azure)
```
Azure
├── Azure Container Apps (microservicios)
├── Azure Database for PostgreSQL
├── Azure Cache for Redis
├── Azure Service Bus
├── Azure Blob Storage
├── Azure OpenAI Service
├── Azure Container Registry
├── Azure Key Vault
└── Azure Monitor + Application Insights
```

---

## Decisiones de Arquitectura

### Por qué microservicios y no monolito modular

Para aprender. Un monolito modular sería la decisión correcta para un equipo pequeño real. Pero microservicios nos obliga a resolver problemas que las entrevistas preguntan: distributed transactions, messaging, service discovery, container orchestration.

### Por qué un servicio por DB y no DB compartida

DB compartida rompe el isolamiento. Si dos servicios comparten DB, un schema migration de uno puede romper el otro. Separar DBs es el principio fundamental de microservicios.

### Por qué MassTransit sobre RabbitMQ directo

MassTransit es una abstracción. El código no sabe si hay RabbitMQ o Azure Service Bus abajo. Cambiamos de uno a otro cambiando config, no código.

### Por qué YARP como gateway

YARP es .NET-native, Microsoft-backed, production-proven en Microsoft.com. Para un equipo .NET, no tiene sentido agregar un proceso externo (Nginx, Kong) cuando YARP es C# puro.

---

## Estado del Sistema

> Este diagrama se actualiza a medida que se construyen los servicios.

| Servicio | Estado | Semana |
|---------|--------|--------|
| Identity Service | ⬜ No iniciado | 1 |
| Catalog Service | ⬜ No iniciado | 2 |
| API Gateway | ⬜ No iniciado | 3 |
| Enrollment Service | ⬜ No iniciado | 4 |
| Notification Service | ⬜ No iniciado | 5 |
| Media Service | ⬜ No iniciado | 9 |
| Progress Service | ⬜ No iniciado | 8 |
| AI Service | ⬜ No iniciado | 13 |
| Frontend (Next.js) | ⬜ No iniciado | 3 |

Leyenda: ⬜ No iniciado | 🟡 En progreso | ✅ Completo
