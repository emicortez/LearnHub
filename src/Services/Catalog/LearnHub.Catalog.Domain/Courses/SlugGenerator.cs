using System.Globalization;
using System.Text;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// Turns a course title into a URL-friendly slug: lowercase ASCII words joined by hyphens.
/// </summary>
public static class SlugGenerator
{
    public static string Generate(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var words = Words(title);
        if (words.Length == 0)
        {
            throw new ArgumentException("The title must contain at least one letter or digit.", nameof(title));
        }

        return string.Join('-', words);
    }

    /// <summary>
    /// True when <paramref name="title"/> has something to build a slug from. It must be valid Unicode.
    /// </summary>
    public static bool HasWords(string title) => Words(title).Length > 0;

    private static string[] Words(string title) =>
        KeepLettersAndDigits(title).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // Lowercases, strips accents (FormD splits "ó" into "o" + a combining mark) and turns
    // every other non-alphanumeric character into a space so it acts as a word separator.
    private static string KeepLettersAndDigits(string title)
    {
        var decomposed = title.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return builder.ToString();
    }
}
