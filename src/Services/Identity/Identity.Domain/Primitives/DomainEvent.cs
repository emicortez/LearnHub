namespace Identity.Domain.Primitives;

// Domain Events describen algo que YA PASÓ en el dominio.
// Tiempo pasado: UserRegistered, EmailChanged — nunca imperativo.
// El Domain los dispara sin saber quién los escucha. Zero coupling.
// Sin dependencia de MediatR — el Domain no sabe de infraestructura.
public abstract record DomainEvent(Guid Id)
{
    protected DomainEvent() : this(Guid.NewGuid()) { }
}
