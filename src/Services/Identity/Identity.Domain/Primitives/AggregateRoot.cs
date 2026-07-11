namespace Identity.Domain.Primitives;

// Un Aggregate es la unidad de consistencia del dominio.
// Solo se puede modificar desde adentro — los setters son privados.
// Acumula eventos internamente; la infraestructura los despacha después.
public abstract class AggregateRoot<TId>
{
    private readonly List<DomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
