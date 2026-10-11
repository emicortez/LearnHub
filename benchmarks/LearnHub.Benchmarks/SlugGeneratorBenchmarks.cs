using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using BenchmarkDotNet.Attributes;

using LearnHub.Catalog.Domain.Courses;

namespace LearnHub.Benchmarks;

/// <summary>
/// Compares the production <see cref="SlugGenerator"/> (a single pass over the characters)
/// with a Regex-based alternative that produces the same slugs.
/// </summary>
[MemoryDiagnoser]
public partial class SlugGeneratorBenchmarks
{
    [Params("Docker", "Introducción a la Programación con C# & .NET 10: de cero a experto")]
    public string Title { get; set; } = "";

    [GlobalSetup]
    public void EnsureSameOutput()
    {
        var expected = SlugGenerator.Generate(Title);
        var actual = WithRegex(Title);
        if (expected != actual)
        {
            throw new InvalidOperationException($"Implementations differ: '{expected}' vs '{actual}'.");
        }
    }

    [Benchmark(Baseline = true)]
    public string CharacterLoop() => SlugGenerator.Generate(Title);

    [Benchmark]
    public string Regex() => WithRegex(Title);

    private static string WithRegex(string title)
    {
        var decomposed = title.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutAccents = new string([.. decomposed.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);

        return NonAlphanumeric().Replace(withoutAccents, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
