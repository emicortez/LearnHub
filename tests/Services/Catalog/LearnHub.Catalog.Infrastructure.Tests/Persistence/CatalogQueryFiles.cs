namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>Reads the versioned query files that the test project copies from Infrastructure/Persistence/Queries.</summary>
public static class CatalogQueryFiles
{
    public static Task<string> ReadAsync(string file) =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Queries", file), TestContext.Current.CancellationToken);
}
