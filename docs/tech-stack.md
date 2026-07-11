# Tech Stack — LearnHub 2026

> Tecnologías elegidas por demanda real en el mercado de Europa y US.
> Todo lo que está acá aparece en job descriptions de empresas top en 2026.
> Se actualiza cuando sale algo importante.

---

## Backend — .NET Core

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **.NET** | 10 LTS | Última versión LTS. Native AOT, performance record, lo que piden en proyectos nuevos. |
| **ASP.NET Core** | 10 | Minimal APIs, Razor, gRPC, SignalR. Base de todo. |
| **EF Core** | 10 | ORM para el write side. Migrations, interceptors, compiled models. Solo para writes en CQRS. |
| **Dapper** | 2.x | SQL mapper ultra-rápido para el read side de CQRS. Sin overhead de change tracking. El read model en SQL puro. |
| **MediatR** | 12.x | CQRS pipeline. Commands, queries, notifications, pipeline behaviors. |
| **FluentValidation** | 11.x | Validación expresiva. Vive en Application, no contamina el domain. |
| **ErrorOr** | 2.x | Result pattern. `ErrorOr<T>` en lugar de exceptions para flujo de errores esperados. Estándar en Clean Arch .NET 2026. |
| **Carter** | 8.x | Organización de Minimal API en módulos. Alternativa limpia a controllers. |
| **Scalar** | 2.x | OpenAPI UI para .NET 10. Reemplaza Swagger/Swashbuckle (deprecado como paquete oficial). |
| **Mapster** | 7.x | Object mapping de alta performance. Source generators. Más rápido que AutoMapper. |
| **Scrutor** | 5.x | Assembly scanning para DI. Decorators sin boilerplate. |
| **Hangfire** | 2.x | Background jobs persistentes. Dashboard integrado. |
| **Quartz.NET** | 3.x | Jobs con expresiones cron complejas. Alternativa a Hangfire para scheduling avanzado. |
| **Serilog** | 4.x | Structured logging. Sinks para Seq, OpenTelemetry, Azure Monitor. |
| **BenchmarkDotNet** | 0.14.x | Profiling de performance con rigor estadístico. |
| **Bogus** | 35.x | Generación de datos realistas para tests y seeds. |

### Por qué Dapper en el read side

EF Core tiene overhead de change tracking, materialización, y lazy loading que no necesitás cuando solo leés. En CQRS, el read model ejecuta SQL directo con Dapper y retorna DTOs. El resultado: queries de lectura 3-10x más rápidos. Un tech lead que usa EF Core para todo no entiende CQRS de verdad.

### Por qué ErrorOr y no exceptions

Las exceptions son para situaciones excepcionales — no para "el email ya existe". Con `ErrorOr<T>`, el handler retorna `ErrorOr<RegisterUserResponse>` que puede ser el resultado o una lista de errores. El caller decide qué hacer. No hay try/catch en el flow de negocio. Es más expresivo, más testeable, y es lo que piden en entrevistas senior en 2026.

---

## Messaging y Eventos

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **MassTransit** | 8.x | Abstracción de messaging sobre RabbitMQ/Azure Service Bus. Sagas incluidas, inbox/outbox built-in. |
| **RabbitMQ** | 4.x | Broker local y staging. Event-driven entre servicios. |
| **Azure Service Bus** | — | Broker en cloud production. Topics, subscriptions, dead-letter, sessions. |
| **Dapr** | 1.x | Distributed Application Runtime. Building blocks declarativos: pub/sub, service invocation, state, secrets, bindings. Se integra nativamente con .NET Aspire. |

### Por qué Dapr

Dapr abstrae los problemas de infraestructura distribuida. En lugar de configurar el cliente de RabbitMQ, el SDK de Redis, y el cliente de Key Vault por separado, Dapr expone una API uniforme via HTTP/gRPC sidecar. Cambiás de RabbitMQ a Azure Service Bus sin tocar código. En 2026, Dapr + .NET Aspire es la combinación que Microsoft empuja para cloud-native enterprise.

---

## Auth y Seguridad

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **OpenIddict** | 5.x | OAuth2/OIDC server embebido en .NET. Ideal para proyectos donde el IdP vive en el mismo stack. |
| **Keycloak** | 25.x | IdP enterprise como servicio separado. SSO, LDAP/AD integration, roles, realm management. Para cuando necesitás un IdP de verdad. |

### OpenIddict vs Keycloak

OpenIddict: lo buildás vos, vive en tu stack, control total. Keycloak: servicio separado, UI de admin, integra con Active Directory, zero-code para SSO. Para LearnHub usamos OpenIddict. En enterprise real, el 80% usa Keycloak o Azure Entra ID.

---

## Event Sourcing

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **Marten** | 7.x | Event store + document DB sobre PostgreSQL. No necesitás una DB extra. El estado es el stream de eventos. Built-in projections para read models. |
| **EventStoreDB** | 24.x | Base de datos diseñada exclusivamente para Event Sourcing. Para cuando necesitás el máximo poder. |

### Por qué Marten primero

Marten corre sobre PostgreSQL — la misma DB que ya tenés. Aprendés Event Sourcing sin agregar infraestructura. Un aggregate en Marten: `session.Events.Append(streamId, events)`. Las projections rebuilden el read model automáticamente. EventStoreDB es más potente pero agrega complejidad operacional. Para aprender, Marten. Para producción con millones de eventos, evaluás EventStoreDB.

---

## Testing

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **xUnit** | 2.9.x | Framework de tests estándar en .NET. |
| **FluentAssertions** | 7.x | Assertions legibles. `result.Should().BeEquivalentTo(expected)`. |
| **Testcontainers** | 4.x | Levanta PostgreSQL, Redis, RabbitMQ reales en Docker para integration tests. |
| **WireMock.Net** | 1.x | HTTP mocking para tests de integración con servicios externos. |
| **Pact.Net** | 5.x | Consumer-driven contract testing entre microservicios. |
| **ArchUnitNET** | 0.22.x | Architecture fitness functions. Escribe tests que fallan si violás la Dependency Rule. |
| **Stryker.NET** | 4.x | Mutation testing. Muta el código y verifica que los tests lo detecten. Mide calidad real de tests. |
| **k6** | 0.55.x | Load testing estándar de industria. Scripts en JS, métricas en tiempo real, integra con Grafana. |
| **Bogus** | 35.x | Datos falsos realistas para tests y seeds. |

### Por qué ArchUnitNET

Un test de arquitectura es código que falla si alguien viola la Dependency Rule. Esto:

```csharp
[Fact]
public void Domain_should_not_depend_on_Infrastructure()
{
    Types.InAssembly(DomainAssembly)
        .Should().NotHaveDependencyOn("Identity.Infrastructure")
        .Because("Domain must have zero external dependencies");
}
```

Sin ArchUnitNET, la arquitectura se deteriora en silencio. Con ArchUnitNET, el CI falla el día que alguien hace `using Identity.Infrastructure` en el domain. Es la forma de hacer que las reglas de arquitectura sean ejecutables.

### Por qué Stryker

Coverage del 80% puede ser falso. Stryker muta el código — cambia `>` por `>=`, remueve una condición — y verifica que al menos un test falle. Si ningún test falla cuando mutás código, los tests no protegen nada. Un tech lead entiende la diferencia entre coverage y calidad de tests.

---

## Infraestructura y Orquestación

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **.NET Aspire** | 9.x / 13.x | **EL framework para microservicios .NET en 2026.** Service discovery, telemetría, dashboards, Dapr integration — todo automático. |
| **Docker** | 27.x | Contenedores para todos los servicios. |
| **Docker Compose** | — | Orquestación local. |
| **Kubernetes** | 1.32.x | Orquestación en cloud. |
| **Helm** | 3.x | Package manager de K8s. Deployments declarativos y versionados. |
| **ArgoCD** | 2.x | GitOps — el estado del cluster vive en Git. ArgoCD aplica automáticamente los cambios. |
| **GitHub Actions** | — | CI/CD pipelines. |
| **Bicep** | — | Infrastructure as Code Microsoft-native para Azure. |
| **Terraform** | 1.x | IaC multi-cloud. Para cuando el stack cruza providers. |

---

## Observabilidad

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **OpenTelemetry** | — | Estándar de observabilidad — logs, metrics, traces. Vendor-neutral. |
| **Grafana** | 11.x | Dashboards de métricas + alertas. |
| **Seq** | 2024.x | Log aggregation para desarrollo. UI de queries sobre logs estructurados. |
| **Azure Monitor + App Insights** | — | Observabilidad en Azure production. |
| **Trivy** | — | Security scanning de imágenes Docker en el pipeline. |
| **OWASP ZAP** | — | Security testing dinámico (DAST) en pipeline de staging. |

---

## Cloud — Azure

| Servicio | Para qué |
|---------|---------|
| **Azure Container Apps** | Deploy de microservicios sin gestionar K8s. Scale to zero. |
| **Azure Kubernetes Service (AKS)** | Kubernetes gestionado. Para entender K8s real. |
| **Azure Blob Storage** | Videos, imágenes, assets estáticos. |
| **Azure Key Vault** | Secrets en producción. Nunca en el código. |
| **Azure App Configuration** | Configuración centralizada + feature flags. |
| **Azure Service Bus** | Messaging production. Topics, dead-letter, sessions. |
| **Azure OpenAI Service** | GPT-4o, embeddings con compliance y SLAs enterprise. |
| **Azure AI Foundry** | Hub unificado para AI projects (2025). |
| **Azure Container Registry** | Docker images en producción. |
| **Azure Front Door** | CDN + WAF + global load balancing para el frontend. |
| **Azure Monitor + App Insights** | Métricas, alertas, traces en producción. |

---

## AI

| Tecnología | Versión | Descripción |
|-----------|---------|-------------|
| **Microsoft AgentFramework** | 1.0 | **EL framework de agentes Microsoft para .NET — GA abril 2026.** Fusión de Semantic Kernel + AutoGen. Orquestación multi-agent, MCP nativo, enterprise-ready. |
| **Microsoft.Extensions.AI** | 9.x | Abstracción estándar de AI. Cambiás de Azure OpenAI a Ollama sin tocar código de negocio. |
| **Azure OpenAI** | — | GPT-4o + embeddings en Azure. Rate limiting, compliance, SLAs. |
| **Qdrant** | 1.x | Vector DB para RAG y semantic search. |
| **Ollama** | latest | Modelos locales (Phi-4, Llama 3.x) para desarrollo sin costos. |
| **MCP (Model Context Protocol)** | spec 2025 | Estándar para conectar agentes con herramientas externas. Implementado nativamente en AgentFramework. |

---

## Frontend

| Tecnología | Versión | Por qué |
|-----------|---------|---------|
| **React** | 19 | 44.7% market share. Server Components estables, use() hook. |
| **Next.js** | 15 | SSR, SSG, RSC, App Router. Estándar para React en producción. |
| **TypeScript** | 5.7+ | Obligatorio en cualquier proyecto serio. |
| **Tailwind CSS** | 4.x | CSS-first, sin config. Estándar de facto. |
| **Vite** | 6.x | Build tool ultra-rápido para SPA. |
| **TanStack Query** | 5.x | Server state — cache, refetch, mutations, optimistic updates. |
| **Zustand** | 5.x | Client state. Simple, sin boilerplate. |
| **React Hook Form + Zod** | 7.x / 3.x | Forms + validación type-safe. |
| **Shadcn/ui** | latest | Componentes headless sobre Radix. Copy-paste, control total. |
| **Framer Motion** | 12.x | Animaciones declarativas. Para UX de producto real. |
| **i18next** | 24.x | Internacionalización. Crítico para el mercado europeo multiidioma. |
| **Storybook** | 8.x | Componentes en aislamiento. Documentación visual + testing. |
| **Vitest** | 3.x | Unit testing integrado con Vite. |
| **React Testing Library** | 16.x | Tests orientados al comportamiento del usuario, no a implementación. |
| **Playwright** | 1.x | E2E testing. Estándar de industria. |

---

## Bases de Datos

| Tecnología | Versión | Uso |
|-----------|---------|-----|
| **PostgreSQL** | 17 | DB principal. Cada servicio tiene la suya (database per service). |
| **Redis** | 7.x | Cache, sesiones, pub/sub, rate limiting, leaderboards. |
| **Qdrant** | 1.x | Vector DB para embeddings, RAG, semantic search. |
| **MongoDB** | 7.x | Documentos no relacionales. Logs, eventos, configuración dinámica. |

---

## Patrones y Arquitecturas

Un tech lead/arquitecto tiene que conocer estos patrones, saber cuándo aplicarlos, y — más importante — cuándo NO.

### Arquitecturales

| Patrón | Cuándo sí | Cuándo no |
|--------|-----------|-----------|
| **Modular Monolith** | Equipo pequeño (<10 devs), dominio no claro todavía, startup | Cuando ya necesitás escala independiente por módulo |
| **Microservices** | Equipos independientes, escala diferenciada por servicio, dominios claros | Equipo pequeño, startup temprana — el overhead operacional mata la productividad |
| **Clean Architecture** | Cualquier sistema que deba vivir más de 2 años | Prototipos, scripts, lambdas simples |
| **Hexagonal Architecture** | Cuando el sistema tiene múltiples adaptadores de entrada/salida | Overkill para APIs simples |
| **Vertical Slice Architecture** | Features muy independientes, equipos feature-oriented | Cuando el dominio tiene reglas transversales fuertes |
| **Event-Driven Architecture** | Desacoplamiento temporal, picos de carga, integración entre servicios | Operaciones que requieren consistencia inmediata |

### Domain-Driven Design (DDD)

| Patrón | Descripción |
|--------|-------------|
| **Aggregates & Aggregate Roots** | Unidad de consistencia. Una sola puerta de entrada. |
| **Value Objects** | Inmutables, iguales por valor. `Email`, `Money`, `Address`. |
| **Domain Events** | Algo que ocurrió. Tiempo pasado. El domain no sabe quién escucha. |
| **Integration Events** | Eventos entre bounded contexts via messaging. |
| **Bounded Contexts** | Cada servicio tiene su propio modelo. No comparte tablas. |
| **Context Mapping** | ACL, Shared Kernel, Customer-Supplier, Conformist. |
| **Domain Services** | Lógica que no pertenece a una entidad específica. |
| **Specifications Pattern** | Reglas de negocio como objetos combinables con AND/OR/NOT. |
| **Factory Method** | Única forma válida de crear un aggregate en estado consistente. |
| **Repository Pattern** | Abstracción de persistencia. El domain nunca sabe de la DB. |

### CQRS & Data

| Patrón | Descripción |
|--------|-------------|
| **CQRS** | Commands (escriben, via EF Core) vs Queries (leen, via Dapper). |
| **Event Sourcing** | Estado = stream de eventos. Inmutable. Auditoría perfecta. |
| **Outbox Pattern** | Evento + datos en misma transacción. Despacho garantizado. |
| **Inbox Pattern** | Idempotencia del consumer. Cada mensaje procesado exactamente una vez. |
| **Materialized View** | Read model precalculado. Actualizado por eventos. |
| **Polyglot Persistence** | Cada servicio usa la DB que mejor se adapta. |
| **Database per Service** | Ningún servicio comparte DB. Independencia real. |
| **Unit of Work** | Múltiples operaciones en una transacción atómica. |

### Microservices & Comunicación

| Patrón | Descripción |
|--------|-------------|
| **API Gateway** | Punto de entrada único. Routing, auth, rate limiting. |
| **Backend for Frontend (BFF)** | Gateway específico por cliente. Evita over/under-fetching. |
| **Service Mesh** | Istio/Linkerd — observabilidad y mTLS en la red. |
| **Sidecar Pattern** | Container auxiliar. Dapr usa este patrón. |
| **Anti-Corruption Layer (ACL)** | Traducción entre bounded contexts. Protege el dominio. |
| **Strangler Fig Pattern** | Migración incremental. No big bang. |
| **Saga — Orchestration** | Orquestador central coordina los pasos. Más control, más acoplamiento. |
| **Saga — Choreography** | Cada servicio reacciona a eventos y publica el siguiente. Sin orquestador. |

### Resiliencia

| Patrón | Descripción |
|--------|-------------|
| **Circuit Breaker** | Falla rápido cuando el downstream está caído. |
| **Retry + Backoff Exponencial** | Reintenta con espera creciente. Jitter para evitar thundering herd. |
| **Bulkhead Isolation** | Aisla recursos por consumer. Un servicio roto no tumba todo. |
| **Timeout** | Falla rápido si la respuesta tarda demasiado. |
| **Fallback** | Respuesta degradada cuando el servicio no está disponible. |
| **Rate Limiting / Throttling** | Controla volumen de requests. Redis como backend. |
| **Health Checks** | Readiness (¿listo para tráfico?), Liveness (¿sigue vivo?). |
| **Graceful Degradation** | El sistema funciona parcialmente cuando algo falla. |

### Seguridad

| Patrón | Descripción |
|--------|-------------|
| **OAuth2 / OpenID Connect** | Delegación de autorización y autenticación federada. |
| **JWT Bearer** | Tokens stateless para autenticación. |
| **Refresh Token Rotation** | Cada uso emite un token nuevo. Detecta robos. |
| **Zero Trust** | Nunca confiar, siempre verificar. mTLS entre servicios. |
| **RBAC / ABAC** | Role-based vs Attribute-based access control. |
| **Secrets Management** | Nunca en el código. Key Vault + env vars en runtime. |
| **OWASP Top 10** | Los 10 riesgos más críticos. Todo tech lead debe conocerlos. |

### Observabilidad

| Patrón | Descripción |
|--------|-------------|
| **Distributed Tracing** | Seguir una request a través de múltiples servicios. |
| **Structured Logging** | Logs como JSON, no strings. Consultables. |
| **RED Metrics** | Rate, Errors, Duration — las 3 métricas que importan. |
| **Correlation ID** | Id único que viaja por todos los servicios. |
| **Architecture Fitness Functions** | Tests automatizados que verifican que la arquitectura no se deteriora. |

### Deployment & CI/CD

| Patrón | Descripción |
|--------|-------------|
| **Blue-Green Deployment** | Dos entornos idénticos. Rollback en segundos. |
| **Canary Release** | Tráfico gradual a la nueva versión. |
| **Rolling Update** | Actualización instancia por instancia. Zero downtime. |
| **Feature Flags** | Deployment ≠ release. Activa features sin redeploy. |
| **GitOps** | Estado del cluster en Git. ArgoCD lo aplica automáticamente. |
| **Infrastructure as Code** | La infraestructura es código versionado y reproducible. |
| **Trunk-Based Development** | Una branch principal. Feature branches cortas. CI permanente. |
| **Shift-Left Security** | Security en el pipeline, no al final del ciclo. |

### Design Patterns (GoF — los que aparecen en entrevistas)

| Patrón | Categoría | Cuándo |
|--------|-----------|--------|
| **Factory Method** | Creacional | Crear objetos sin exponer lógica de creación. Aggregates. |
| **Builder** | Creacional | Objetos complejos paso a paso. Test data builders. |
| **Strategy** | Comportamiento | Algoritmos intercambiables en runtime. Pricing, notificaciones. |
| **Observer** | Comportamiento | Notificar múltiples dependientes. Domain Events. |
| **Command** | Comportamiento | Encapsular una acción como objeto. CQRS commands. |
| **Decorator** | Estructural | Agregar comportamiento sin modificar. Pipeline behaviors. |
| **Proxy** | Estructural | Intermediario. Caching, lazy loading, logging. |
| **Adapter** | Estructural | Traducir una interfaz a otra. ACL en DDD. |
| **Facade** | Estructural | Interfaz simple para subsistema complejo. |
| **Chain of Responsibility** | Comportamiento | Pipeline de handlers. MediatR behaviors, middleware. |
| **Specification** | Dominio | Reglas de negocio combinables con AND/OR/NOT. |
| **Template Method** | Comportamiento | Esqueleto de algoritmo. Pasos en subclases. |

### AI Patterns

| Patrón | Descripción |
|--------|-------------|
| **RAG** | Ingest → Embed → Store → Retrieve → Generate. El LLM con tu data. |
| **Tool Use / Function Calling** | El agente decide qué herramienta invocar. |
| **ReAct (Reason + Act)** | Razona, actúa, observa resultado, repite. Loop de agente. |
| **Multi-Agent Workflow** | Agentes especializados coordinados por un orquestador. |
| **Semantic Cache** | Cachear respuestas similares semánticamente. Reduce costos. |
| **Model Router** | Modelo correcto según complejidad: Haiku para simple, GPT-4o para complejo. |
| **Guardrails** | Content filtering, validación de output, defensa contra prompt injection. |
| **Embedding Pipeline** | Chunk → Embed → Upsert → Index. Flujo de ingesta RAG. |
| **Context Window Management** | Chunking, summarization, sliding window para conversaciones largas. |

### Frontend Patterns

| Patrón | Descripción |
|--------|-------------|
| **Container/Presentational** | Lógica (container) separada de UI (presentational). |
| **Compound Components** | Estado compartido implícito via context. |
| **Custom Hooks** | Lógica reutilizable encapsulada. |
| **Server Components vs Client Components** | RSC en Next.js — qué renderiza el server, qué el cliente. |
| **Optimistic UI** | Actualiza UI antes de confirmar con el server. UX fluida. |
| **Stale-While-Revalidate** | Muestra data vieja mientras revalida en background. |
| **Atomic Design** | Atoms → Molecules → Organisms → Templates → Pages. |
| **Error Boundary** | Captura errores en el árbol. Fallback UI. |

---

## Versiones — Última Actualización

`Abril 2026`

> Para verificar versiones antes de empezar un bloque:
> - .NET: https://dotnet.microsoft.com/download
> - Microsoft AgentFramework: https://github.com/microsoft/agent-framework
> - MassTransit: https://masstransit.io/
> - Marten: https://martendb.io/
> - React/Next.js: https://nextjs.org/blog
