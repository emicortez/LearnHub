using System.Xml.Linq;

namespace LearnHub.Architecture.Tests;

// Guards against the distributed monolith: a service may reference its own projects and the
// building blocks, never another service's projects (ADR 0002). Integration goes through
// messages and HTTP contracts, not shared code. New services are covered automatically.
public sealed class ServiceIsolationTests
{
    private static readonly DirectoryInfo ServicesDirectory =
        new(Path.Combine(FindRepositoryRoot(), "src", "Services"));

    [Fact]
    public void Services_do_not_reference_projects_of_other_services()
    {
        var projects = ServicesDirectory.EnumerateFiles("*.csproj", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(projects);

        var violations = projects
            .SelectMany(project => ProjectReferences(project)
                .Where(reference => IsInAnotherService(project, reference))
                .Select(reference => $"{project.Name} -> {Path.GetFileName(reference)}"))
            .Order()
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Services must integrate through messages or HTTP, not project references: {string.Join(", ", violations)}");
    }

    private static IEnumerable<string> ProjectReferences(FileInfo project) =>
        XDocument.Load(project.FullName)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Select(include => Path.GetFullPath(
                Path.Combine(project.DirectoryName!, include.Replace('\\', Path.DirectorySeparatorChar))));

    private static bool IsInAnotherService(FileInfo project, string referencePath)
    {
        var referenceService = ServiceName(referencePath);
        return referenceService is not null && referenceService != ServiceName(project.FullName);
    }

    // The service is the first folder under src/Services; anything outside it has no service.
    private static string? ServiceName(string path)
    {
        var relative = Path.GetRelativePath(ServicesDirectory.FullName, path);
        return relative.StartsWith("..", StringComparison.Ordinal)
            ? null
            : relative.Split(Path.DirectorySeparatorChar)[0];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LearnHub.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("LearnHub.slnx not found above the test output directory.");
    }
}
