using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// Expected business failures raised by the <see cref="Course"/> aggregate and its value objects.
/// </summary>
public static class CourseErrors
{
    public static readonly Error TitleRequired =
        new("catalog.course.title_required", "The course title is required.");

    public static readonly Error TitleTooLong =
        new("catalog.course.title_too_long", "The course title must be at most 120 characters long.");

    public static readonly Error NegativeAmount =
        new("catalog.money.negative_amount", "The amount must not be negative.");

    public static readonly Error TooManyDecimals =
        new("catalog.money.too_many_decimals", "The amount must have at most 2 decimal places.");

    public static readonly Error InvalidCurrency =
        new("catalog.money.invalid_currency", "The currency must be a 3-letter code.");

    public static readonly Error LessonTitleRequired =
        new("catalog.lesson.title_required", "The lesson title is required.");

    public static readonly Error InvalidDuration =
        new("catalog.lesson.invalid_duration", "The lesson duration must be greater than zero.");

    public static readonly Error NotDraft =
        new("catalog.course.not_draft", "The course must be in draft status for this operation.");

    public static readonly Error NoLessons =
        new("catalog.course.no_lessons", "The course must have at least one lesson to be published.");

    public static readonly Error NotPublished =
        new("catalog.course.not_published", "The course must be published for this operation.");

    public static readonly Error Archived =
        new("catalog.course.archived", "The course is archived and cannot be modified.");
}
