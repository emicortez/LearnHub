# Lección — Semana 1, Día 1
**Fecha:** 28 Abril 2026
**Tema:** Clean Architecture + DDD + SOLID
**Proyecto:** Identity.Domain de LearnHub

---

## Resumen de la lección

Hoy construimos la capa más importante de un sistema bien diseñado: el **Domain**. Aprendimos por qué el dominio es el corazón de la arquitectura, qué es DDD, qué principios SOLID aplicamos, y por qué cada decisión que tomamos importa en una entrevista senior.

---

## 1. Clean Architecture

### ¿Qué es?

Clean Architecture (Robert C. Martin — "Uncle Bob") es una forma de organizar el código para que sea **independiente de frameworks, bases de datos, y herramientas externas**. El negocio vive en el centro y no sabe nada de lo que está afuera.

### Las capas

```
          ┌─────────────────────────────────┐
          │              API                │  ← HTTP, controllers, endpoints
          │   ┌─────────────────────────┐   │
          │   │      Infrastructure     │   │  ← EF Core, BCrypt, SendGrid
          │   │   ┌─────────────────┐   │   │
          │   │   │   Application   │   │   │  ← CQRS, handlers, validators
          │   │   │  ┌───────────┐  │   │   │
          │   │   │  │  Domain   │  │   │   │  ← Entities, Value Objects, Events
          │   │   │  └───────────┘  │   │   │
          │   │   └─────────────────┘   │   │
          │   └─────────────────────────┘   │
          └─────────────────────────────────┘
```

### La regla fundamental — La Regla de Dependencias

> **Las dependencias solo pueden apuntar hacia adentro. Nunca hacia afuera.**

```
Domain ← Application ← Infrastructure ← API
```

- El **Domain** no sabe que existe Application, ni Infrastructure, ni API
- El **Application** conoce el Domain, pero no la Infrastructure
- La **Infrastructure** implementa los contratos que definen Domain y Application
- La **API** es la capa más externa — sabe de todo, pero no tiene lógica

### ¿Por qué importa?

Si el Domain depende de EF Core o BCrypt, cuando necesitás cambiar la base de datos o el algoritmo de hashing, **tocás el corazón del sistema**. Con Clean Architecture, cambiás solo Infrastructure y el resto no se entera.

### En LearnHub — lo que construimos hoy

```
Identity.Domain/          ← ZERO dependencias externas. Solo .NET BCL.
Identity.Application/     ← Depende de Domain. Tiene MediatR, FluentValidation.
Identity.Infrastructure/  ← Depende de Domain + Application. Tiene BCrypt, EF Core.
Identity.API/             ← Depende de todos. Tiene JWT, endpoints.
```

---

## 2. Domain-Driven Design (DDD)

DDD es una forma de modelar el software que pone el **lenguaje del negocio** en el centro del código. Los objetos en el código deben reflejar los conceptos reales del dominio.

### 2.1 Entity vs Value Object

| | Entity | Value Object |
|---|---|---|
| **Identidad** | Única (tiene un Id) | No tiene Id |
| **Igualdad** | Por Id | Por valor |
| **Mutabilidad** | Puede cambiar (setters privados) | Inmutable |
| **Ejemplo** | `User` | `Email`, `UserId` |

**Entity en código:**
```csharp
// Dos Users con el mismo email son DISTINTOS si tienen diferente Id
var user1 = User.Create("a@a.com", hash, "Juan", "Pérez");
var user2 = User.Create("a@a.com", hash, "Juan", "Pérez");
// user1 != user2  →  porque tienen diferente UserId
```

**Value Object en código:**
```csharp
// Dos Emails con el mismo valor son IGUALES
var email1 = Email.Create("test@test.com");
var email2 = Email.Create("test@test.com");
// email1 == email2  →  porque el VALOR es el mismo
```

**¿Por qué `Email` y no `string`?**

Con `string` podés tener `User.Email = ""` o `User.Email = "esto no es email"` — el compilador no te avisa. Con `Email.Create(...)`, si el formato es inválido, se lanza una `DomainException` **antes de que el objeto exista**. Un `Email` que existe en el sistema siempre es válido. Esto se llama **"making illegal states unrepresentable"** — uno de los conceptos más importantes de diseño.

```csharp
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;  // constructor privado

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("user.email.empty", "Email is required.");

        // Si llegamos acá, el Email ES válido — garantizado
        return new Email(value.Trim().ToLowerInvariant());
    }

    // Igualdad por VALOR, no por referencia
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 2.2 Aggregate Root

Un **Aggregate** es un grupo de objetos que se tratan como una unidad. El **Aggregate Root** es la única puerta de entrada — nadie puede modificar el aggregate sin pasar por él.

```csharp
// User es el Aggregate Root
public sealed class User : AggregateRoot<UserId>
{
    // Setters PRIVADOS — nadie puede cambiar el estado desde afuera
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    private User() { }  // Solo para EF Core

    // Factory method — la ÚNICA forma de crear un User válido
    public static User Create(string email, string passwordHash, ...)
    {
        // Validaciones → si algo falla, el objeto NUNCA existe
        var user = new User { ... };
        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));
        return user;
    }
}
```

**¿Por qué factory method y no `new User(...)`?**

Con constructor público, nada impide crear un User en estado inválido:
```csharp
var user = new User();       // compila sin error
// user.Email = null         // estado roto — el sistema lo acepta
```

Con `User.Create(...)`, si algo es inválido → `DomainException`. Si el método retorna → el objeto está **garantizado** en estado válido. Nunca existió un `User` roto en el sistema.

### 2.3 Domain Events

Los Domain Events describen algo que **ya ocurrió** en el dominio. Siempre en tiempo pasado: `UserRegistered`, `CoursePublished`, `PaymentProcessed`.

**Flujo completo:**
```
1. User.Create()
       └─→ RaiseDomainEvent(new UserRegisteredEvent(userId, email))
           El evento queda en _domainEvents (lista interna del aggregate)

2. UserRepository.AddAsync(user)
       └─→ Guarda el User en PostgreSQL

3. Infrastructure (después del save)
       └─→ Lee user.DomainEvents
       └─→ Despacha cada evento via MediatR
       └─→ user.ClearDomainEvents()

4. Handlers en Application
       └─→ SendWelcomeEmailHandler → envía email
       └─→ IndexUserHandler → indexa para búsqueda
```

**¿Por qué despachar DESPUÉS del save?**

Si el save falla, los eventos no se despachan. No mandás email de bienvenida a un usuario que no existe en la DB. La consistencia está garantizada.

**El Domain no sabe quién escucha sus eventos** — eso es desacoplamiento real. Si mañana agregás un nuevo handler, el Domain no cambia.

---

## 3. Principios SOLID

Los cinco principios que todo arquitecto de software debe conocer de memoria y poder explicar con ejemplos reales.

### S — Single Responsibility Principle

> Un módulo debe tener una, y solo una, razón para cambiar.

```csharp
// ❌ MAL — esta clase tiene múltiples responsabilidades
public class UserService
{
    public void Register(string email, string password) { ... }   // lógica de negocio
    public void SaveToDatabase(User user) { ... }                  // persistencia
    public void SendWelcomeEmail(string email) { ... }             // notificación
    public string HashPassword(string plain) { ... }               // seguridad
}

// ✅ BIEN — cada clase tiene una sola responsabilidad
public class User { ... }                    // lógica de dominio
public class UserRepository { ... }          // persistencia
public class EmailService { ... }            // notificación
public class BCryptPasswordHasher { ... }    // hashing
```

En LearnHub: `User` solo sabe ser un usuario. `BCryptPasswordHasher` solo sabe hashear. `UserRepository` solo sabe persistir.

---

### O — Open/Closed Principle

> El código debe estar abierto para extensión, cerrado para modificación.

```csharp
// ❌ MAL — para agregar un nuevo método de pago, modificás la clase existente
public class PaymentProcessor
{
    public void Process(string method, decimal amount)
    {
        if (method == "stripe") { ... }
        else if (method == "paypal") { ... }
        // Para agregar MercadoPago → modificás esta clase (riesgoso)
    }
}

// ✅ BIEN — para agregar MercadoPago, solo agregás una nueva clase
public interface IPaymentGateway
{
    Task ProcessAsync(decimal amount);
}

public class StripeGateway : IPaymentGateway { ... }
public class PayPalGateway : IPaymentGateway { ... }
public class MercadoPagoGateway : IPaymentGateway { ... }  // nueva, sin tocar las demás
```

---

### L — Liskov Substitution Principle

> Si B hereda de A, podés usar B en cualquier lugar donde esperabas A sin romper nada.

```csharp
public abstract class Shape
{
    public abstract double Area();
}

public class Circle : Shape
{
    public override double Area() => Math.PI * radius * radius;
}

public class Square : Shape
{
    public override double Area() => side * side;
}

// Funciona con cualquier Shape — no importa si es Circle o Square
public void PrintArea(Shape shape)
{
    Console.WriteLine(shape.Area());  // nunca se rompe
}
```

El principio se viola cuando una subclase cambia el comportamiento esperado de formas inesperadas (como el ejemplo clásico del rectángulo/cuadrado que rompe la invariante de tamaños independientes).

---

### I — Interface Segregation Principle

> Es mejor tener muchas interfaces pequeñas que una grande y genérica.

```csharp
// ❌ MAL — una interfaz que obliga a implementar métodos que no necesitás
public interface IRepository<T>
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<T>> SearchAsync(string query);  // no todos los repos necesitan esto
    Task<int> CountAsync();
}

// ✅ BIEN — interfaces específicas
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
}

public interface IWriteRepository<T>
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
}
```

En LearnHub: `IUserRepository` define solo lo que el `User` domain necesita — no un repositorio genérico con 20 métodos.

---

### D — Dependency Inversion Principle

> Los módulos de alto nivel no deben depender de los de bajo nivel. Ambos deben depender de abstracciones.

**Este es el que aplicamos hoy en tiempo real:**

```csharp
// ❌ ANTES (mal) — Domain depende directamente de BCrypt (Infrastructure)
public static User Create(string email, string plainPassword, ...)
{
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword); // dep. directa ❌
}

// ✅ DESPUÉS (bien) — Domain define el contrato, Infrastructure lo implementa
// En Domain:
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hashedPassword);
}

// En Infrastructure:
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plain) => BCrypt.Net.BCrypt.HashPassword(plain);
    public bool Verify(string plain, string hash) => BCrypt.Net.BCrypt.Verify(plain, hash);
}

// En Application (handler):
public class RegisterUserHandler
{
    private readonly IPasswordHasher _hasher;  // depende de la abstracción
    // Si mañana cambiamos BCrypt por Argon2 → solo tocamos Infrastructure
}
```

**Flujo de dependencias:**
```
Domain (define IPasswordHasher)
    ↑
Application (usa IPasswordHasher)
    ↑
Infrastructure (implementa con BCrypt)
```

El Domain no conoce BCrypt. BCrypt conoce IPasswordHasher. La dependencia está invertida.

---

## 4. Errores que corregimos hoy (aprendizaje real)

### Error 1: Domain dependía de MediatR

El primer `DomainEvent.cs` importaba `using MediatR`. Lo corregimos: el Domain define sus propios tipos base sin dependencias externas.

**Lección:** MediatR es infrastructure para el Domain. Si el Domain sabe de MediatR, viola la Dependency Rule.

### Error 2: User.cs importaba BCrypt

El primer `User.cs` llamaba `BCrypt.Net.BCrypt.HashPassword(...)` directamente. Lo corregimos con `IPasswordHasher`.

**Lección:** Cada vez que estés en Domain y sientas el impulso de importar algo, preguntate: "¿esto es lógica de negocio o es un detalle técnico?" Si es detalle técnico → va a Infrastructure.

---

## 5. Conceptos clave para entrevistas

| Concepto | Definición rápida |
|---------|------------------|
| **Clean Architecture** | Capas concéntricas, dependencias solo hacia adentro |
| **Dependency Rule** | Domain ← Application ← Infrastructure ← API |
| **Entity** | Objeto con identidad única, igual por Id |
| **Value Object** | Objeto sin identidad, igual por valor, inmutable |
| **Aggregate Root** | Única puerta de entrada a un aggregate |
| **Factory Method** | Única forma de crear un objeto válido |
| **Domain Event** | Algo que ya ocurrió, tiempo pasado, el domain no sabe quién escucha |
| **Making illegal states unrepresentable** | Si el objeto existe, está garantizado en estado válido |
| **Dependency Inversion** | Alto nivel depende de abstracción, no de implementación |
| **SRP** | Una clase, una razón para cambiar |

---

## 6. Lo que construimos

```
Identity.Domain/
├── Primitives/
│   ├── AggregateRoot.cs    ← base para todos los aggregates
│   ├── ValueObject.cs      ← igualdad por valor
│   └── DomainEvent.cs      ← algo que pasó en el dominio
├── Entities/
│   └── User.cs             ← aggregate root con factory method
├── ValueObjects/
│   ├── Email.cs            ← valida formato, inmutable
│   └── UserId.cs           ← wrappea Guid con semántica
├── Events/
│   └── UserRegisteredEvent.cs
├── Interfaces/
│   ├── IUserRepository.cs  ← contrato de persistencia
│   └── IPasswordHasher.cs  ← contrato de hashing
└── Errors/
    ├── DomainException.cs
    └── UserErrors.cs       ← códigos: "user.email.empty"
```

**Resultado:** `Identity.Domain` compila con **0 dependencias externas de NuGet**. Solo .NET Base Class Library. Eso es Clean Architecture aplicado.

---

## Próxima lección (Día 2)

**Tema:** CQRS + MediatR — Application Layer
- ¿Qué es CQRS y por qué separa Commands de Queries?
- `RegisterUserCommand` + `RegisterUserCommandHandler`
- Pipeline Behaviors: logging, validación, caching
- Ver cómo Application orquesta Domain + Infrastructure sin acoplarse a ninguno
