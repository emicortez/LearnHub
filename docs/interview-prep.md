# Interview Prep — LearnHub

> Las preguntas que las empresas europeas y americanas hacen en 2026 para roles senior .NET fullstack.
> Organizado por área. Marcar cuando tenés la respuesta dominada.

---

## Arquitectura y Diseño

- [ ] Explicá Clean Architecture. ¿Cuál es la regla de dependencias?
- [ ] ¿Cuándo usarías microservicios y cuándo un monolito?
- [ ] ¿Qué es DDD? ¿Qué es un Aggregate Root?
- [ ] ¿Qué es CQRS? ¿Cuáles son sus tradeoffs?
- [ ] ¿Cuándo usarías Event Sourcing?
- [ ] Diseñá un API Gateway. ¿Qué responsabilidades tiene?
- [ ] ¿Qué es el patrón Strangler Fig?
- [ ] ¿Qué es un Bounded Context en DDD?

---

## Microservices

- [ ] ¿Cómo te comunicás entre microservicios? ¿Cuándo sync, cuándo async?
- [ ] ¿Qué es el Saga Pattern? ¿Orchestration vs Choreography?
- [ ] ¿Cómo manejás transacciones distribuidas sin 2PC?
- [ ] ¿Qué es el Outbox Pattern y por qué importa?
- [ ] ¿Cómo asegurás que un consumer de mensajes es idempotente?
- [ ] ¿Qué pasa si un servicio se cae? ¿Cómo diseñás para resilencia?
- [ ] ¿Cómo manejarías un cambio de schema de un microservicio sin downtime?
- [ ] ¿Cómo hacés service discovery?

---

## Cloud y DevOps

- [ ] Explicá tu proceso de CI/CD desde un commit hasta producción
- [ ] ¿Qué es Kubernetes? ¿Por qué existe?
- [ ] ¿Cuál es la diferencia entre un Pod y un Deployment?
- [ ] ¿Qué es un Helm chart?
- [ ] ¿Cómo manejás secretos en la nube? ¿Qué NO hacés?
- [ ] ¿Qué es Infrastructure as Code? ¿Usaste Terraform o Bicep?
- [ ] Blue-green vs canary deployment — ¿cuándo cada uno?
- [ ] ¿Cómo escalás un servicio bajo carga?

---

## Observabilidad

- [ ] ¿Cuáles son los tres pilares de observabilidad?
- [ ] ¿Qué es distributed tracing? ¿Cómo lo implementarías?
- [ ] Un usuario reporta que la app es lenta. ¿Cómo investigás?
- [ ] ¿Qué es un SLO? ¿Qué métricas monitorearías para una API?
- [ ] ¿Diferencia entre health check y readiness/liveness probe?

---

## Bases de Datos

- [ ] ¿Cuándo usarías una DB por servicio vs DB compartida?
- [ ] ¿Qué es el problema N+1 en EF Core y cómo lo resolvés?
- [ ] ¿Cuándo usarías un índice? ¿Cómo sabés si una query es lenta?
- [ ] ¿Qué es ACID? ¿Y BASE?
- [ ] ¿Cuándo usarías Redis en lugar de una DB relacional?
- [ ] ¿Qué es cache stampede y cómo lo evitás?
- [ ] ¿Qué es una vector database? ¿Cuándo la usarías?

---

## .NET Específico

- [ ] ¿Qué es el .NET Aspire y qué problema resuelve?
- [ ] ¿Cuál es la diferencia entre IHostedService y BackgroundService?
- [ ] ¿Qué es Minimal API vs Controllers? ¿Cuándo cada uno?
- [ ] ¿Cómo funciona el middleware pipeline en ASP.NET Core?
- [ ] ¿Qué es gRPC y cuándo lo usarías sobre REST?
- [ ] ¿Cómo funciona el DI container en .NET? ¿Scoped vs Transient vs Singleton?
- [ ] ¿Qué mejoras trae .NET 10 sobre versiones anteriores?
- [ ] ¿Qué es Native AOT y cuándo conviene usarlo?

---

## Frontend

- [ ] ¿Cuál es la diferencia entre Server Components y Client Components en Next.js?
- [ ] ¿Cuándo usarías SSR, SSG, o CSR?
- [ ] ¿Qué es el App Router de Next.js y cómo difiere del Pages Router?
- [ ] ¿Cómo manejarías el estado global en una app React grande?
- [ ] ¿Qué es TanStack Query y qué problema resuelve?
- [ ] ¿Cuál es la diferencia entre useEffect y una Server Action?

---

## AI e Inteligencia Artificial

- [ ] ¿Qué es RAG y cómo funciona?
- [ ] ¿Cuál es la diferencia entre RAG y fine-tuning? ¿Cuándo usar cada uno?
- [ ] ¿Qué es Semantic Kernel y para qué lo usarías?
- [ ] ¿Qué es un embedding?
- [ ] ¿Qué es un AI Agent?
- [ ] ¿Qué es MCP (Model Context Protocol)?
- [ ] ¿Cómo gestionarías los costos de una API de LLM en producción?
- [ ] ¿Qué son los guardrails en AI? ¿Por qué importan?
- [ ] ¿Qué es prompt injection y cómo lo mitigás?

---

## Testing

- [ ] Explicá la testing pyramid
- [ ] ¿Cuándo mockearías y cuándo usarías Testcontainers?
- [ ] ¿Qué es contract testing? ¿Para qué sirve en microservicios?
- [ ] ¿Cómo testearías un Saga?
- [ ] ¿Qué es mutation testing?

---

## System Design (preguntas de entrevista típicas)

- [ ] Diseñar YouTube / sistema de video streaming
- [ ] Diseñar un sistema de pagos
- [ ] Diseñar Uber (geolocalización, matching)
- [ ] Diseñar un feed de noticias (como Twitter/LinkedIn)
- [ ] Diseñar LearnHub (vas a poder explicar este de memoria)
- [ ] Diseñar un sistema de notificaciones a escala
- [ ] Diseñar un sistema de autenticación OAuth2
- [ ] Diseñar un sistema de búsqueda

---

## Behavioral Questions (STAR method)

- [ ] Contame sobre un proyecto del que estés orgulloso
- [ ] ¿Cuándo tomaste una decisión técnica difícil? ¿Cuál fue el resultado?
- [ ] ¿Cuándo tuviste un conflicto con un compañero de equipo?
- [ ] ¿Cuándo fallaste en algo importante? ¿Qué aprendiste?
- [ ] ¿Cómo manejas prioridades cuando todo es urgente?
- [ ] ¿Cómo te mantenés actualizado con nuevas tecnologías?
- [ ] ¿Cuál fue el bug más difícil que debuggeaste?
- [ ] ¿Cómo explicarías una decisión técnica compleja a alguien no técnico?

---

## Preguntas para Hacerle a la Empresa

> Estas preguntas demuestran que pensás en serio y separás el bien del mal:

- ¿Cómo es el proceso de deploy a producción? ¿Con qué frecuencia hacen releases?
- ¿Cómo manejan los incidents en producción? ¿Tienen on-call rotation?
- ¿Cómo está estructurado el testing? ¿Tienen CI/CD robusto?
- ¿Cuál es el mayor desafío técnico que enfrenta el equipo ahora?
- ¿Cómo se toman las decisiones de arquitectura?
- ¿Hay deuda técnica significativa? ¿Cómo la manejan?
