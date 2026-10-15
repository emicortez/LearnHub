using System.Reflection;

namespace LearnHub.Architecture.Tests;

// Enforces the dependency rule: domain code depends only on the .NET base class library
// and the shared kernel, never on infrastructure, frameworks, or other services.
public sealed class DomainDependencyTests
{
    private const string SharedKernel = "LearnHub.SharedKernel";

    public static TheoryData<string> DomainAssemblies =>
    [
        SharedKernel,
        "LearnHub.Catalog.Domain",
    ];

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_assembly_depends_only_on_the_base_class_library_and_shared_kernel(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);

        var forbidden = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !IsAllowed(name))
            .Order()
            .ToList();

        Assert.True(
            forbidden.Count == 0,
            $"{assemblyName} must not depend on: {string.Join(", ", forbidden)}");
    }

    private static bool IsAllowed(string name) =>
        name is "System" or "netstandard" or "mscorlib" or SharedKernel
        || name.StartsWith("System.", StringComparison.Ordinal);
}
