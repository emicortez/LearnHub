# Semana 1 — DDD + Identity Service

**Período:** 28 Abril – 4 Mayo 2026
**Estado:** ⬜ No iniciado

---

## Objetivo de la Semana

Salir del CRUD. La mayoría de los devs modelan sus aplicaciones con "anemic models" — objetos que son solo bolsas de datos. Esta semana aprendemos a poner la lógica de negocio donde corresponde: en el dominio.

---

## Concepto Principal: Domain-Driven Design

### El problema que DDD resuelve

Cuando el dominio vive en el servicio o el controlador:
```csharp
// ❌ Anemic model — el User no sabe nada de sí mismo
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsActive { get; set; }
}

// La lógica vive en un servicio cualquiera
public class UserService
{
    public void Deactivate(User user) { user.IsActive = false; }
    public void ChangeEmail(User user, string email) { user.Email = email; }
}
```

Con DDD:
```csharp
// ✅ Rich domain model — el User protege sus invariantes
public class User : AggregateRoot
{
    public UserId Id { get; private set; }
    public Email Email { get; private set; }  // Value Object
    public HashedPassword Password { get; private set; }  // Value Object

    private User() { }  // Para EF Core

    public static User Create(string email, string plainPassword)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = Email.Create(email),     // valida formato
            Password = HashedPassword.Create(plainPassword)  // hashea
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));
        return user;
    }

    public void ChangeEmail(Email newEmail)
    {
        if (Email == newEmail) return;
        Email = newEmail;
        RaiseDomainEvent(new UserEmailChangedEvent(Id, newEmail));
    }
}
```

### Value Objects

Son inmutables. Son iguales por valor, no por referencia.

```csharp
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidEmailException();
        if (!value.Contains('@')) throw new InvalidEmailException();
        return new Email(value.ToLowerInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### Domain Events

Algo importante pasó en el dominio. Notificamos a quién le interese.

```csharp
// Se dispara dentro del Aggregate
user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));

// Alguien lo maneja (puede ser en otro servicio)
public class SendWelcomeEmailOnUserRegistered
    : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, ...)
    {
        await _emailService.SendWelcomeEmail(notification.Email);
    }
}
```

---

## Build: Identity Service

### Estructura del proyecto

```
identity-service/
├── Identity.Domain/
│   ├── Entities/
│   │   └── User.cs
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   ├── UserId.cs
│   │   └── HashedPassword.cs
│   ├── Events/
│   │   └── UserRegisteredEvent.cs
│   ├── Errors/
│   │   └── UserErrors.cs
│   ├── Interfaces/
│   │   └── IUserRepository.cs
│   └── Primitives/
│       ├── AggregateRoot.cs
│       ├── ValueObject.cs
│       └── DomainEvent.cs
├── Identity.Application/
│   ├── Commands/
│   │   ├── RegisterUser/
│   │   │   ├── RegisterUserCommand.cs
│   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   └── RegisterUserCommandValidator.cs
│   │   └── LoginUser/
│   │       ├── LoginUserCommand.cs
│   │       └── LoginUserCommandHandler.cs
│   ├── Queries/
│   │   └── GetUserById/
│   │       └── GetUserByIdQuery.cs
│   └── Services/
│       └── IJwtService.cs
├── Identity.Infrastructure/
│   ├── Persistence/
│   │   ├── IdentityDbContext.cs
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs
│   │   └── Configurations/
│   │       └── UserConfiguration.cs
│   └── Services/
│       └── JwtService.cs
└── Identity.API/
    ├── Endpoints/
    │   └── AuthEndpoints.cs
    ├── Program.cs
    └── appsettings.json
```

### Tareas del día a día

**Lunes 28 Abr:**
- [ ] Crear la solución con .NET Aspire
- [ ] Crear proyectos: Domain, Application, Infrastructure, API
- [ ] Implementar `AggregateRoot`, `ValueObject`, `DomainEvent` en Domain

**Martes 29 Abr:**
- [ ] Implementar `Email`, `UserId`, `HashedPassword` value objects
- [ ] Implementar `User` aggregate con `Create()` estático
- [ ] `UserRegisteredEvent` domain event

**Miércoles 30 Abr:**
- [ ] `RegisterUserCommand` + handler
- [ ] `LoginUserCommand` + handler
- [ ] Configurar MediatR en Application

**Jueves 1 May:**
- [ ] `IdentityDbContext` + EF Core configurations
- [ ] `UserRepository`
- [ ] Migración inicial
- [ ] `JwtService` — generar access + refresh tokens

**Viernes 2 May:**
- [ ] Endpoints de register y login en Minimal API
- [ ] Configurar .NET Aspire AppHost
- [ ] Configurar PostgreSQL via Aspire

**Sábado 3 May:**
- [ ] Refresh token endpoint
- [ ] Unit tests del domain (User, Value Objects)
- [ ] Integration test: registro de usuario end-to-end

**Domingo 4 May:**
- [ ] System design: diseñar un sistema de autenticación OAuth2 desde cero
- [ ] Revisar la semana, completar el tracker en progress.md

---

## Preguntas de Entrevista que Cubre Esta Semana

1. ¿Qué es Clean Architecture? ¿Cuál es la "regla de dependencias"?
2. ¿Qué es un Aggregate Root? ¿Por qué existe?
3. ¿Cuál es la diferencia entre un Entity y un Value Object?
4. ¿Qué son los Domain Events? ¿Cómo se diferencian de los eventos de integración?
5. ¿Por qué el domain no debería depender de la base de datos?

---

## Recursos

- [Domain-Driven Design — Eric Evans (conceptos)](https://martinfowler.com/tags/domain%20driven%20design.html)
- [Clean Architecture — Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Aspire docs](https://learn.microsoft.com/dotnet/aspire)
- [MediatR docs](https://github.com/jbogard/MediatR)

---

## Aprendizajes y Gotchas

> Completar a medida que avanzás. Este es el log de lo que aprendiste.

-
-
-

---

## Estado Final de la Semana

- [ ] Identity Service corre en local via Aspire
- [ ] Register + Login funcionando
- [ ] Refresh token implementado
- [ ] Tests del domain pasando
- [ ] Podés explicar DDD en 3 minutos
