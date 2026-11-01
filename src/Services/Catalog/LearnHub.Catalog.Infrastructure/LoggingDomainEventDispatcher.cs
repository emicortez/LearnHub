using LearnHub.Catalog.Application;
using LearnHub.SharedKernel;

using Microsoft.Extensions.Logging;

namespace LearnHub.Catalog.Infrastructure;

/// <summary>
/// Records each committed domain event in the log. Nothing consumes the events yet; Wolverine replaces this when
/// messaging arrives.
/// </summary>
public sealed partial class LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            LogDispatched(domainEvent.GetType().Name);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dispatched domain event {EventName}")]
    private partial void LogDispatched(string eventName);
}
