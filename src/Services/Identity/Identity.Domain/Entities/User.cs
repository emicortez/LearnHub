using Identity.Domain.Errors;
using Identity.Domain.Events;
using Identity.Domain.Primitives;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

// User es un Aggregate Root — la única puerta de entrada a todo el aggregate.
// Los setters son PRIVADOS: el estado solo cambia a través de los métodos del objeto.
// El Domain nunca depende de infraestructura (BCrypt, EF Core, etc.).
public sealed class User : AggregateRoot<UserId>
{
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // Para EF Core

    // Factory method: la única forma de crear un User válido.
    // Recibe el hash ya generado — el Domain no sabe cómo hashear.
    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("user.firstname.empty", "First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("user.lastname.empty", "Last name is required.");

        var user = new User
        {
            Id = UserId.New(),
            Email = Email.Create(email),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));

        return user;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
    }
}
