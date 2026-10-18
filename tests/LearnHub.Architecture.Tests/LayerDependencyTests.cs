using System.Reflection;

namespace LearnHub.Architecture.Tests;

// Enforces the Clean Architecture dependency rule between layers (ADR 0003):
// Domain <- Application <- Infrastructure <- Api. Inner layers never reach outward.
public sealed class LayerDependencyTests
{
    public static TheoryData<string> ApplicationAssemblies =>
    [
        "LearnHub.Catalog.Application",
    ];

    public static TheoryData<string> InfrastructureAssemblies =>
    [
        "LearnHub.Catalog.Infrastructure",
    ];

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Application_does_not_depend_on_infrastructure_api_or_frameworks(string assemblyName)
    {
        var forbidden = ReferencedAssemblyNames(assemblyName)
            .Where(name => name.EndsWith(".Infrastructure", StringComparison.Ordinal)
                || name.EndsWith(".Api", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            forbidden.Count == 0,
            $"{assemblyName} must not depend on: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [MemberData(nameof(InfrastructureAssemblies))]
    public void Infrastructure_does_not_depend_on_api(string assemblyName)
    {
        var forbidden = ReferencedAssemblyNames(assemblyName)
            .Where(name => name.EndsWith(".Api", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            forbidden.Count == 0,
            $"{assemblyName} must not depend on: {string.Join(", ", forbidden)}");
    }

    // The compiler drops references to assemblies that no code uses, so this sees real dependencies only.
    private static IEnumerable<string> ReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Order();
}
