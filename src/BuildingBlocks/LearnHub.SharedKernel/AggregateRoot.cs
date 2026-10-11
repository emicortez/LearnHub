namespace LearnHub.SharedKernel;

/// <summary>
/// Entry point of a consistency boundary. Records the domain events raised by its behavior;
/// infrastructure dispatches them after saving and then calls <see cref="ClearDomainEvents"/>.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
