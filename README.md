# LearnHub — Interview Preparation Project

> Plataforma de aprendizaje online (Udemy-style) construida para preparar entrevistas en el mercado laboral europeo y americano.
> El objetivo no es lanzar un producto — es aprender haciendo con tecnologías reales de 2026.

## Objetivo

Estar listo para entrevistas senior .NET fullstack en **Europa y Estados Unidos** para **septiembre 2026**.

## La Estrategia

En lugar de estudiar teoría, construimos una aplicación real y compleja que nos obliga a aprender cada tecnología en contexto. Cada semana agrega un servicio, un patrón, o una funcionalidad nueva.

## Documentación

| Archivo | Descripción |
|---------|-------------|
| [Plan Maestro](docs/plan.md) | Cronograma completo — 18 semanas, día a día |
| [Tech Stack](docs/tech-stack.md) | Todas las tecnologías con versiones y justificación |
| [Arquitectura](docs/architecture.md) | Diseño del sistema completo de LearnHub |
| [Progreso](docs/progress.md) | Tracker semanal — qué está hecho, qué falta |
| [Prep Entrevistas](docs/interview-prep.md) | Temas por área con estado de preparación |
| [Roadmap IA](docs/ai-roadmap.md) | Path específico de integración de IA |

## Semanas semanales

Las semanas se documentan en [`docs/weekly/`](docs/weekly/) con:
- Concepto teórico de la semana
- Qué se construyó en LearnHub
- Aprendizajes y gotchas
- Preguntas de entrevista que cubre

## Stack de un vistazo

```
Backend:  .NET 10 + ASP.NET Core + .NET Aspire
Frontend: React 19 + Next.js 15 + TypeScript + Tailwind 4
Gateway:  YARP
DB:       PostgreSQL 17 + Redis 7 + Qdrant
Msgs:     RabbitMQ → Azure Service Bus
Cloud:    Azure (Container Apps / AKS, OpenAI, Blob, Key Vault)
AI:       Semantic Kernel 1.x + RAG + Microsoft.Extensions.AI
DevOps:   Docker + GitHub Actions + Terraform/Bicep
Obs:      OpenTelemetry + Grafana + Seq
```

## Servicios de LearnHub

```
identity-service     → Auth, usuarios, roles (OAuth2/OIDC)
catalog-service      → Cursos, categorías, instructores
media-service        → Videos, uploads, streaming (Azure Blob)
enrollment-service   → Compras, suscripciones
progress-service     → Avance, certificados
notification-service → Emails, push, real-time (SignalR)
ai-service           → Recomendaciones, RAG, chatbot
api-gateway          → YARP — entry point único
```
