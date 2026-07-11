using Identity.Domain.Primitives;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    Email Email) : DomainEvent;
