using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// The title of a course: non-blank, trimmed, at most 120 characters long, with at least one letter or digit to
/// build its slug from, and without the null character, which PostgreSQL cannot store in text.
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

        if (trimmed.Contains('\0', StringComparison.Ordinal))
        {
            return CourseErrors.TitleInvalidCharacter;
        }

        if (!trimmed.Any(char.IsLetterOrDigit))
        {
            return CourseErrors.TitleWithoutLetters;
        }

        return new CourseTitle(trimmed);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
