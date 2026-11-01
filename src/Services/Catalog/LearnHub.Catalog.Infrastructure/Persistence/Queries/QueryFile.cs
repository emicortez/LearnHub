using System.Reflection;

namespace LearnHub.Catalog.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads a versioned query embedded in this assembly, so the SQL that runs and the SQL under review are one file.
/// </summary>
internal static class QueryFile
{
    public static string Read(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The query '{name}' is not embedded.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
