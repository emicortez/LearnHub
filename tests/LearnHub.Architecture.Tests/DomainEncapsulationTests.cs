using System.Reflection;

using LearnHub.SharedKernel;

namespace LearnHub.Architecture.Tests;

// Fitness functions for the domain model: entities and value objects change only through
// their own behavior and are created only through factory methods, so invariants cannot be bypassed.
public sealed class DomainEncapsulationTests
{
    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.Instance;

    public static TheoryData<string> DomainAssemblies =>
    [
        "LearnHub.Catalog.Domain",
    ];

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_objects_have_no_public_setters(string assemblyName)
    {
        var violations = DomainObjectTypes(assemblyName)
            .SelectMany(type => type.GetProperties(InstanceMembers)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{type.Name}.{property.Name}"))
            .Order()
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Change state through behavior, not public setters: {string.Join(", ", violations)}");
    }

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_objects_have_no_public_constructors(string assemblyName)
    {
        var violations = DomainObjectTypes(assemblyName)
            .Where(type => type.GetConstructors(InstanceMembers).Length > 0)
            .Select(type => type.Name)
            .Order()
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Create domain objects through factory methods, not public constructors: {string.Join(", ", violations)}");
    }

    private static IEnumerable<Type> DomainObjectTypes(string assemblyName) =>
        Assembly.Load(assemblyName)
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsSubclassOf(typeof(Entity)) || type.IsSubclassOf(typeof(ValueObject)));
}
