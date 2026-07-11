# Examen — Semana 1
**Tema:** Clean Architecture + DDD + CQRS + Docker + API Gateway
**Fecha:** completar al final de la semana (4 Mayo 2026)
**Formato:** 10 preguntas abiertas + 20 multiple choice

> ⚠️ No mirar las respuestas antes de responder. El objetivo es detectar gaps reales.

---

## Parte 1 — Preguntas abiertas (10)

*Respondé con tus propias palabras. No vale copiar del doc.*

1. Explicá Clean Architecture en 2 minutos como si se lo explicaras a alguien que nunca lo vio. ¿Cuál es la regla que no se puede romper?

2. ¿Cuál es la diferencia entre una Entity y un Value Object? Dá un ejemplo concreto de cada uno en LearnHub.

3. ¿Por qué `User.Create()` es un factory method estático y no un constructor público? ¿Qué garantiza?

4. Describí el flujo completo de un Domain Event: ¿dónde se origina, dónde se acumula, quién lo despacha, y por qué después del save?

5. Explicá el Principio de Inversión de Dependencias con el ejemplo de `IPasswordHasher` de LearnHub.

6. ¿Qué es CQRS? ¿Cuál es la diferencia entre un Command y una Query? ¿Cuándo tiene sentido usarlo?

7. ¿Qué es un Dockerfile multi-stage y por qué es mejor que un Dockerfile simple?

8. ¿Para qué sirve un API Gateway? ¿Qué responsabilidades tiene el de LearnHub (YARP)?

9. Explicá la diferencia entre comunicación síncrona y asíncrona entre microservicios. ¿Cuándo usás cada una?

10. ¿Qué es "making illegal states unrepresentable"? Dá dos ejemplos del código de LearnHub donde aplicamos este principio.

---

## Respuestas — Preguntas abiertas

> ⚠️ Solo leer después de responder por escrito.

---

**1. Clean Architecture**

Clean Architecture organiza el código en capas concéntricas donde el negocio está en el centro. La idea central es que el código más importante — las reglas de negocio — no debe depender de detalles técnicos como frameworks, bases de datos, o librerías externas.

La regla fundamental que no se puede romper es la **Regla de Dependencias**: las dependencias solo apuntan hacia adentro.

```
Domain ← Application ← Infrastructure ← API
```

El Domain no sabe que existe EF Core, ni BCrypt, ni ASP.NET. Si mañana cambiás PostgreSQL por MongoDB, el Domain no se entera — solo tocás Infrastructure. El beneficio real es que el código de negocio puede vivir, crecer, y testearse independientemente de cualquier tecnología.

---

**2. Entity vs Value Object**

Una **Entity** tiene identidad única — se compara por su Id. Dos entities con los mismos datos son objetos distintos si tienen diferente Id.

Un **Value Object** no tiene identidad — se compara por valor. Dos instancias con los mismos datos son intercambiables y equivalentes.

Ejemplos en LearnHub:
- `User` es una Entity → dos usuarios con el mismo email son personas distintas porque tienen diferente `UserId`
- `Email` es un Value Object → `Email("a@a.com")` y otro `Email("a@a.com")` son exactamente iguales

Los Value Objects además son **inmutables** — si el email cambia, no modificás el objeto, creás uno nuevo. Esto elimina bugs de estado compartido.

---

**3. Factory method `User.Create()`**

Con un constructor público `new User(...)`, cualquier parte del código puede crear un `User` en estado inválido — email vacío, sin contraseña, inactivo desde el inicio. El compilador no se queja.

`User.Create()` es el único punto de entrada. Adentro valida todo, asigna el Id, marca el usuario como activo, y dispara el Domain Event. Si algo es inválido → `DomainException`, el objeto nunca existe. Si el método retorna → el `User` está **garantizado** en estado válido.

El constructor privado sin parámetros existe solo para que EF Core pueda reconstruir objetos desde la base de datos. No es para uso directo.

---

**4. Flujo completo de un Domain Event**

```
1. ORIGEN: User.Create()
   └─→ RaiseDomainEvent(new UserRegisteredEvent(userId, email))

2. ACUMULACIÓN: _domainEvents (lista privada en AggregateRoot)
   El evento queda guardado en memoria, nadie lo ve todavía.

3. PERSISTENCIA: UserRepository.AddAsync(user)
   └─→ Guarda el User en PostgreSQL. Si falla → el flujo se detiene acá.

4. DESPACHO: Infrastructure (después del save exitoso)
   └─→ Lee user.DomainEvents
   └─→ Publica cada evento via MediatR
   └─→ Llama user.ClearDomainEvents()

5. HANDLERS en Application (en paralelo o secuencia)
   └─→ SendWelcomeEmailHandler → envía email
   └─→ IndexUserHandler → indexa para búsqueda de AI
```

Se despacha **después del save** porque si la persistencia falla, no queremos efectos secundarios (emails enviados a usuarios que no existen). El Domain no sabe quién escucha — eso es desacoplamiento real.

---

**5. Dependency Inversion con IPasswordHasher**

El Principio de Inversión de Dependencias dice que los módulos de alto nivel no deben depender de los de bajo nivel — ambos deben depender de abstracciones.

- **Alto nivel** = Domain (lógica de negocio más importante)
- **Bajo nivel** = BCrypt (detalle técnico de infraestructura)
- **Abstracción** = `IPasswordHasher` (el contrato)

En lugar de que `User.Create()` llame `BCrypt.HashPassword(...)` directamente, el Domain define la interfaz `IPasswordHasher` con los métodos `Hash()` y `Verify()`. Infrastructure implementa esa interfaz con BCrypt.

```
Domain (define IPasswordHasher) ← Infrastructure (implementa con BCrypt)
```

El Domain no conoce BCrypt. Si mañana BCrypt tiene una vulnerabilidad y cambiás a Argon2, tocás solo un archivo en Infrastructure. El Domain ni se entera.

---

**6. CQRS**

CQRS (Command Query Responsibility Segregation) separa las operaciones de escritura (Commands) de las de lectura (Queries).

- **Command**: modifica estado, no retorna datos. Ejemplo: `RegisterUserCommand`, `PublishCourseCommand`
- **Query**: lee estado, no lo modifica. Ejemplo: `GetCourseByIdQuery`, `SearchCoursesQuery`

¿Cuándo tiene sentido?
- Cuando las necesidades de lectura y escritura son muy distintas (leer necesita joins complejos, escribir necesita validaciones pesadas)
- Cuando querés escalar lectura y escritura de forma independiente
- En sistemas donde la carga de lectura es mucho mayor que la de escritura

No tiene sentido en CRUDs simples donde no hay lógica de negocio real — sería over-engineering.

---

**7. Dockerfile multi-stage**

Un Dockerfile simple copia todo el SDK de .NET dentro de la imagen final — resulta en imágenes de 700MB+ llenas de herramientas de desarrollo que no necesitás en producción.

Multi-stage usa varias etapas:
```dockerfile
# Etapa 1: BUILD — usa el SDK completo para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . .
RUN dotnet publish -o /app

# Etapa 2: RUNTIME — imagen mínima, solo el runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=build /app .   # solo copia el output compilado
ENTRYPOINT ["dotnet", "Identity.API.dll"]
```

La imagen final solo contiene el runtime de .NET y los binarios compilados — sin código fuente, sin SDK, sin herramientas. Resultado: imágenes de ~100MB vs 700MB. Más seguras (menos superficie de ataque), más rápidas de descargar y deployar.

---

**8. API Gateway**

El API Gateway es el único punto de entrada al sistema desde el exterior. Los clientes (frontend, mobile, third-parties) nunca llaman directamente a los microservicios.

Responsabilidades en LearnHub (YARP):
- **Routing**: recibe `/api/courses/123` y lo redirige al `catalog-service`
- **Autenticación**: valida el JWT una sola vez antes de forwarding — los servicios internos confían en el gateway
- **Rate limiting**: limita requests por usuario para proteger de abuso
- **SSL termination**: gestiona HTTPS en un solo punto

Sin gateway, cada microservicio debería implementar autenticación, rate limiting y SSL por separado — duplicación masiva. Además el frontend necesitaría saber las URLs de 8 servicios distintos.

---

**9. Comunicación síncrona vs asíncrona**

**Síncrona (HTTP/gRPC):** el servicio A llama al B y espera la respuesta antes de continuar.
- Usás cuando: el servicio A necesita datos del B para responder al cliente
- Ejemplo: el frontend pide los detalles de un curso → API Gateway → Catalog Service → respuesta inmediata
- Desventaja: si el Catalog Service está caído, el request falla

**Asíncrona (RabbitMQ/Azure Service Bus):** el servicio A publica un evento y sigue su camino. El B lo procesa cuando puede.
- Usás cuando: el servicio A notifica algo que pasó y no necesita esperar resultado
- Ejemplo: usuario se inscribe → `UserEnrolled` event → Notification Service envía email (no bloquea la respuesta al usuario)
- Ventaja: si el Notification Service está caído, el mensaje queda en la cola y se procesa cuando vuelve

Regla general: si necesitás la respuesta para continuar → síncrono. Si solo notificás que algo pasó → asíncrono.

---

**10. Making illegal states unrepresentable**

El principio significa diseñar los tipos de forma tal que un objeto en estado inválido directamente no pueda existir — el compilador o el runtime te lo impiden antes.

Dos ejemplos en LearnHub:

**Ejemplo 1 — `Email` Value Object:**
Si email fuera un `string`, podrías tener `user.Email = ""` o `user.Email = "notanemail"` sin que nadie te detenga. Con `Email.Create("...")` que lanza `DomainException` si el formato es inválido, es físicamente imposible tener un `Email` mal formado en el sistema. Si el objeto `Email` existe → el email es válido. Garantizado.

**Ejemplo 2 — `User.Create()` factory method:**
Con constructor público podés crear `new User()` sin email, sin contraseña, inactivo. Con el factory method privado, el único camino para crear un `User` es `User.Create(email, hash, firstName, lastName)` que valida todo. Si alguna validación falla → excepción. Si el método retorna un objeto → ese objeto está en estado 100% válido.

---

## Parte 2 — Multiple Choice (20)

*Marcá la opción correcta.*

**1.** En Clean Architecture, ¿cuál es la dirección correcta de las dependencias?

- A) API ← Infrastructure ← Application ← Domain
- B) Domain ← Application ← Infrastructure ← API ✓
- C) Domain → Application → Infrastructure → API
- D) Todas las capas pueden depender entre sí libremente

---

**2.** Un Value Object se diferencia de una Entity porque:

- A) No tiene ningún tipo de identificador y es igual por valor ✓
- B) Tiene un Id único y se compara por referencia
- C) Solo puede usarse dentro de un Aggregate Root
- D) Es siempre mutable

---

**3.** ¿Por qué el constructor de `User` es `private`?

- A) Para que EF Core no pueda instanciarlo
- B) Para obligar el uso del factory method `User.Create()` y garantizar estado válido ✓
- C) Por convención de C# en record types
- D) Para evitar herencia

---

**4.** ¿Qué viola este código?

```csharp
public class User : AggregateRoot<UserId>
{
    public Email Email { get; set; }  // setter público
}
```

- A) El principio Open/Closed
- B) El encapsulamiento del Aggregate Root — el estado puede cambiar desde afuera ✓
- C) El Single Responsibility Principle
- D) Nada, es código válido

---

**5.** Los Domain Events se despachan DESPUÉS de persistir el aggregate porque:

- A) MediatR solo funciona después de que la transacción cierra
- B) Si el save falla, los efectos secundarios (emails, notificaciones) no deben ejecutarse ✓
- C) Es un requisito de .NET Aspire
- D) Los eventos se pierden si se despachan antes

---

**6.** `IPasswordHasher` vive en el proyecto `Identity.Domain` porque:

- A) BCrypt es una dependencia del dominio
- B) El Domain define el contrato; Infrastructure provee la implementación (DIP) ✓
- C) Es más fácil testear así
- D) Es un requisito de Clean Architecture para todos los servicios

---

**7.** ¿Qué principio SOLID se viola aquí?

```csharp
public class UserService
{
    public void Register() { ... }
    public void SendEmail() { ... }
    public void SaveToDb() { ... }
    public void HashPassword() { ... }
}
```

- A) Open/Closed Principle
- B) Liskov Substitution Principle
- C) Single Responsibility Principle ✓
- D) Interface Segregation Principle

---

**8.** En CQRS, ¿cuál es la diferencia fundamental entre Command y Query?

- A) Los Commands son más rápidos que las Queries
- B) Los Commands modifican estado y no retornan datos; las Queries leen y no modifican ✓
- C) Los Commands van a la DB principal; las Queries a una réplica
- D) No hay diferencia real, es solo semántica

---

**9.** `GetEqualityComponents()` en un Value Object sirve para:

- A) Comparar objetos por referencia de memoria
- B) Serializar el objeto a JSON
- C) Definir qué propiedades determinan la igualdad entre dos instancias ✓
- D) Implementar el patrón Observer

---

**10.** ¿Qué es `.NET Aspire` en LearnHub?

- A) Un framework de testing para microservicios
- B) El orquestador que levanta todos los servicios e infraestructura localmente ✓
- C) Un reemplazo de Docker para .NET
- D) Una librería de logging distribuido

---

**11.** Un Aggregate Root garantiza que:

- A) El aggregate no puede persistirse en una base de datos NoSQL
- B) Toda modificación al aggregate pasa por un único punto de entrada ✓
- C) El aggregate nunca tiene más de una Entity
- D) Los Value Objects del aggregate son siempre públicos

---

**12.** El error codes format `user.email.empty` en LearnHub sigue el patrón:

- A) `framework.library.error`
- B) `service.entity.problem` ✓
- C) `layer.class.method`
- D) `module.type.severity`

---

**13.** ¿Cuándo un Domain Event NO debería usarse?

- A) Cuando algo importante cambió en el dominio
- B) Cuando querés desacoplar dos partes del sistema
- C) Cuando necesitás hacer una consulta síncrona a otro servicio y esperar la respuesta ✓
- D) Cuando un aggregate cambia de estado

---

**14.** En la Dependency Rule de Clean Architecture, ¿qué capa puede depender de todas las demás?

- A) Domain
- B) Application
- C) Infrastructure
- D) API ✓

---

**15.** ¿Qué hace `ClearDomainEvents()` y cuándo se llama?

- A) Borra los eventos de la base de datos
- B) Limpia la lista interna de eventos del aggregate después de que Infrastructure los despachó ✓
- C) Cancela eventos que todavía no fueron procesados
- D) Se llama automáticamente en el constructor del aggregate

---

**16.** El principio "making illegal states unrepresentable" significa:

- A) Usar excepciones para todos los errores de validación
- B) Diseñar los tipos de forma que un objeto en estado inválido no pueda existir ✓
- C) Usar enums en lugar de strings para todos los estados
- D) Nunca permitir valores null en el sistema

---

**17.** ¿Por qué existe un `NuGet.config` en la raíz de LearnHub?

- A) Para agregar el feed de paquetes corporativos de la empresa
- B) Para sobreescribir el config global que incluye un feed de Telerik que requiere credenciales y rompe los restores ✓
- C) Es requerido por .NET Aspire para funcionar
- D) Para configurar el caché local de paquetes NuGet

---

**18.** En el Principio de Inversión de Dependencias, "invertir" significa:

- A) Que la Infrastructure depende del Domain, no al revés ✓
- B) Que las clases deben heredar en lugar de componer
- C) Que las interfaces deben estar en Infrastructure
- D) Que los constructores deben ser privados

---

**19.** ¿Cuál es el propósito del proyecto `LearnHub.ServiceDefaults`?

- A) Definir los DTOs compartidos entre microservicios
- B) Proveer configuración compartida de Aspire: OpenTelemetry, health checks, service discovery ✓
- C) Contener los modelos de base de datos compartidos
- D) Implementar el API Gateway

---

**20.** Un pipeline behavior de MediatR se usa para:

- A) Reemplazar los repositorios en los tests
- B) Interceptar todos los commands/queries para aplicar lógica transversal (logging, validación, caching) ✓
- C) Configurar las rutas HTTP de los endpoints
- D) Gestionar las migraciones de EF Core

---

## Respuestas

> *Completar sección de respuestas después de hacer el examen. No mirar antes.*

| # | Mi respuesta | Correcta | ✅/❌ |
|---|-------------|---------|------|
| Open 1 | | | |
| Open 2 | | | |
| Open 3 | | | |
| Open 4 | | | |
| Open 5 | | | |
| Open 6 | | | |
| Open 7 | | | |
| Open 8 | | | |
| Open 9 | | | |
| Open 10 | | | |
| MC 1 | | B | |
| MC 2 | | A | |
| MC 3 | | B | |
| MC 4 | | B | |
| MC 5 | | B | |
| MC 6 | | B | |
| MC 7 | | C | |
| MC 8 | | B | |
| MC 9 | | C | |
| MC 10 | | B | |
| MC 11 | | B | |
| MC 12 | | B | |
| MC 13 | | C | |
| MC 14 | | D | |
| MC 15 | | B | |
| MC 16 | | B | |
| MC 17 | | B | |
| MC 18 | | A | |
| MC 19 | | B | |
| MC 20 | | B | |

**Puntaje:** ___/30
