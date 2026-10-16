using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// The title of a course: non-blank, trimmed, and at most 120 characters long.
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

        return new CourseTitle(trimmed);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
