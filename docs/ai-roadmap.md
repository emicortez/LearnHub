# AI Roadmap — LearnHub

> El path específico para integrar inteligencia artificial en aplicaciones .NET.
> En 2026 esto es diferenciador. Developers que entienden AI + backend sólido
> están en el top 10% del mercado.

---

## El panorama actual

```
Lo que piden las empresas en 2026:
  ✅ Integrar LLMs en aplicaciones existentes
  ✅ Construir features RAG sobre datos propios
  ✅ AI Agents que automatizan flujos de trabajo
  ✅ Entender costos, latencia, y limitaciones de AI en prod
  ✅ MCP Protocol — conectar sistemas con LLMs
  ❌ NO te piden entrenar modelos (eso es Data Science / MLOps)
  ❌ NO te piden implementar transformers desde cero
```

---

## Semana 13 — Semantic Kernel + Fundamentos

### Qué es Semantic Kernel

Framework open-source de Microsoft para integrar LLMs en aplicaciones .NET, Python, y Java. Es el estándar para el ecosistema Microsoft en 2026.

```
LLM Provider                     Tu Aplicación
(Azure OpenAI,  ──────────────>  Semantic Kernel
 OpenAI,                         └── Plugins (funciones que el LLM puede llamar)
 Ollama)                         └── Planners (el LLM decide qué hacer)
                                 └── Memory (contexto persistente)
                                 └── Process Framework (flujos multi-paso)
```

### Microsoft.Extensions.AI

La capa de abstracción por encima de Semantic Kernel y cualquier proveedor AI. Permite escribir código una vez y cambiar de Azure OpenAI a Ollama a cualquier provider cambiando solo la configuración.

```csharp
// Con Microsoft.Extensions.AI — no depende del provider
IChatClient chatClient = services.GetRequiredService<IChatClient>();
var response = await chatClient.CompleteAsync(messages);
```

### Build en LearnHub — Semana 13

```
ai-service/
├── Domain/
│   └── (no domain objects complejos — AI es infrastructure)
├── Application/
│   ├── Commands/
│   │   └── IndexCourseContent/    ← cuando se publica un curso
│   └── Queries/
│       ├── ChatWithBot/            ← chat completion
│       └── SearchCourses/         ← semantic search
├── Infrastructure/
│   ├── SemanticKernel/
│   │   ├── Plugins/
│   │   │   └── CatalogPlugin.cs   ← el LLM puede buscar cursos
│   │   └── KernelFactory.cs
│   ├── VectorStore/
│   │   └── QdrantVectorStore.cs
│   └── AI/
│       └── AzureOpenAIClient.cs
└── API/
    └── Endpoints/
        ├── ChatEndpoints.cs
        └── SearchEndpoints.cs
```

---

## Semana 14 — RAG (Retrieval-Augmented Generation)

### Qué problema resuelve RAG

Un LLM sabe sobre el mundo hasta su fecha de corte. NO sabe:
- Qué cursos tiene LearnHub
- El contenido específico de tus cursos
- Actualizaciones recientes

RAG le da al LLM contexto real de TUS datos antes de que responda.

### Pipeline RAG completo

```
FASE 1 — INGESTA (cuando se publica un curso):
  Curso publicado
      ↓
  Extraer texto (título, descripción, lecciones, transcripción)
      ↓
  Chunking (dividir en fragmentos de ~500 tokens con overlap)
      ↓
  Generar embeddings (Azure OpenAI text-embedding-3-small)
      ↓
  Almacenar vectores en Qdrant + metadata (courseId, chunkIndex, etc.)

FASE 2 — RETRIEVAL (cuando un usuario hace una búsqueda):
  Query del usuario: "quiero aprender microservicios en .NET"
      ↓
  Generar embedding de la query
      ↓
  Buscar los K chunks más similares en Qdrant (cosine similarity)
      ↓
  Opcional: Reranking (reordenar resultados)
      ↓
  Construir prompt: context + query + instrucciones del sistema
      ↓
  Llamar al LLM con el contexto
      ↓
  Respuesta fundamentada en los datos reales
```

### Chunking strategies

| Strategy | Cuándo usarla |
|----------|--------------|
| Fixed size (500 tokens, overlap 50) | General purpose, fácil de implementar |
| By sentence | Mejor preservación de contexto semántico |
| By section/heading | Documentación estructurada |
| Semantic chunking | Más costoso, mejores resultados |

### RAG vs Fine-tuning

| | RAG | Fine-tuning |
|---|---|---|
| **Cuándo** | Datos cambian frecuentemente | Comportamiento/estilo del modelo |
| **Costo** | Bajo (solo embeddings + queries) | Alto (training GPU) |
| **Velocidad** | Datos nuevos disponibles al instante | Horas/días de retraining |
| **Caso LearnHub** | ✅ Catálogo cambia constantemente | ❌ Innecesario |

---

## Semana 15 — Vector Databases

### Por qué los DBs relacionales no funcionan para búsqueda semántica

```sql
-- Búsqueda tradicional (keyword)
SELECT * FROM courses WHERE description LIKE '%microservices%'
-- "microservicios" no matchea. "distributed systems" tampoco.

-- Búsqueda semántica (vector)
-- "quiero aprender sobre sistemas distribuidos" → encuentra cursos de
-- microservices, event-driven, distributed computing aunque no use esas palabras exactas
```

### Qdrant en LearnHub

```
Collections:
  course_chunks
    ├── vector: float[1536]     ← embedding de text-embedding-3-small
    ├── payload:
    │   ├── course_id: uuid
    │   ├── chunk_index: int
    │   ├── text: string        ← el chunk original
    │   ├── section: string     ← "description" | "lesson_title" | "lesson_content"
    │   ├── level: string       ← "beginner" | "intermediate" | "advanced"
    │   └── category_id: uuid
    └── id: uuid
```

### Búsqueda híbrida (lo más usado en producción)

```
Query: "microservicios .NET nivel intermedio"
  ↓
Búsqueda vectorial (semántica) ←── encuentra conceptualmente similar
  +
Filtro de metadata (category = "backend", level = "intermediate")
  ↓
Resultados combinados → Reranking → Top 5 cursos
```

---

## Semana 16 — AI Agents + MCP + Producción

### AI Agents

Un AI Agent puede:
1. **Razonar** sobre qué hacer
2. **Ejecutar herramientas** (funciones reales de tu app)
3. **Observar** los resultados
4. **Continuar** o terminar

```
Usuario: "¿Puedo estudiar microservicios con .NET? No tengo mucho tiempo."
    ↓
AI Agent decide:
  1. Llamar CatalogPlugin.SearchCourses("microservicios .NET")
  2. Llamar ProgressPlugin.GetUserEnrollments(userId)
  3. Llamar CatalogPlugin.GetCourseDuration(courseId)
  4. Construir respuesta: "Sí, tenés 3 opciones. El más corto es X con 8 horas..."
```

### MCP (Model Context Protocol)

Protocolo estándar (Anthropic, adoptado por toda la industria en 2025) para conectar LLMs con herramientas externas. Es el estándar de facto en 2026.

```
Antes de MCP:
  Cada LLM → integración custom → cada herramienta
  N × M integraciones

Con MCP:
  Cualquier LLM → MCP client → MCP server → herramientas
  N + M integraciones
```

Implementaremos un **MCP Server** básico para LearnHub que expone:
- `search_courses` — buscar cursos
- `get_course_details` — detalles de un curso
- `check_enrollment` — verificar si está inscripto
- `get_user_progress` — progreso del usuario

### Streaming responses

```
Sin streaming:
  Usuario hace request → espera 3-5 segundos → ve la respuesta entera

Con streaming (SSE):
  Usuario hace request → empieza a ver el texto token por token → UX mucho mejor
```

```csharp
// Server-Sent Events en ASP.NET Core
app.MapGet("/api/ai/chat/stream", async (HttpContext context, ...) =>
{
    context.Response.Headers["Content-Type"] = "text/event-stream";

    await foreach (var chunk in kernel.InvokeStreamingAsync(...))
    {
        await context.Response.WriteAsync($"data: {chunk}\n\n");
        await context.Response.Body.FlushAsync();
    }
});
```

### Cost Management en producción

| Táctica | Ahorro estimado |
|---------|----------------|
| Cache de respuestas similares (Redis) | 30-60% |
| Usar modelos pequeños para tareas simples (Phi-4 local) | 80-90% |
| Limite de tokens por request | Control directo |
| Rate limiting por usuario | Control directo |
| Monitoreo de uso por feature | Visibilidad |

```
Jerarquía de modelos por costo:
  GPT-4o          → tareas complejas, chat principal
  GPT-4o-mini     → búsqueda, clasificación, summaries
  text-embedding-3-small → embeddings (baratísimo)
  Phi-4 (local)   → desarrollo y testing ($0)
```

### Guardrails y Content Safety

```
Request pipeline en el AI Service:
  User input
      ↓
  [Input validation] ← validar longitud, caracteres
      ↓
  [Content safety check] ← Azure AI Content Safety API
      ↓
  [Prompt construction] ← system prompt + context + query
      ↓
  LLM call
      ↓
  [Output validation] ← verificar que la respuesta tiene formato esperado
      ↓
  [Content safety check] ← también en el output
      ↓
  Response to user
```

---

## Recursos

### Documentación oficial
- [Semantic Kernel docs](https://learn.microsoft.com/semantic-kernel)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai)
- [MCP Specification](https://modelcontextprotocol.io)
- [Qdrant docs](https://qdrant.tech/documentation)

### Para desarrollo local (sin costos)
- [Ollama](https://ollama.ai) — correr modelos localmente
- Modelos recomendados: `phi4`, `llama3.2`, `nomic-embed-text` (embeddings)

### Conceptos para entender antes del código
- Qué es un transformer (conceptual, no matemático)
- Qué es un token
- Temperatura y top-p en LLMs
- Context window y sus limitaciones
