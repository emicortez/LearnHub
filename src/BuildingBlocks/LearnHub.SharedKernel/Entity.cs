namespace LearnHub.SharedKernel;

/// <summary>
/// Base class for domain objects with identity: two entities are equal when they are
/// of the same type and have the same <see cref="Id"/>, whatever their other values.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity needs a non-empty id.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; }

    public bool Equals(Entity? other) =>
        other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
