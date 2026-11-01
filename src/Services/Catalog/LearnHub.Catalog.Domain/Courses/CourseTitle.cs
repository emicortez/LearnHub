using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// The title of a course: non-blank, trimmed, at most 120 characters long, valid Unicode that PostgreSQL can store,
/// and something a slug can be built from.
/// </summary>
public sealed class CourseTitle : ValueObject
{
    private CourseTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CourseTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CourseErrors.TitleRequired;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 120)
        {
            return CourseErrors.TitleTooLong;
        }

        if (trimmed.Contains('\0', StringComparison.Ordinal) || !IsValidUnicode(trimmed))
        {
            return CourseErrors.TitleInvalidCharacter;
        }

        // Asked of the generator, not of the raw text: it normalizes first, which can make or unmake a letter.
        if (!SlugGenerator.HasWords(trimmed))
        {
            return CourseErrors.TitleWithoutLetters;
        }

        return new CourseTitle(trimmed);
    }

    public override string ToString() => Value;

    private static bool IsValidUnicode(string value)
    {
        try
        {
            _ = value.Normalize();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
