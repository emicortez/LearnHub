using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application;

/// <summary>
/// Delivers committed domain events. Delivery is at most once: a crash between the commit and this call loses the
/// events, until messaging brings an outbox.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
