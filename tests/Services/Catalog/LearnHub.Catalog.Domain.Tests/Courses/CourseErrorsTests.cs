using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Tests.Courses;

public sealed class CourseErrorsTests
{
    [Fact]
    public void Input_failures_are_validation_errors()
    {
        Assert.All(
            [
                CourseErrors.InstructorRequired,
                CourseErrors.TitleRequired,
                CourseErrors.TitleTooLong,
                CourseErrors.NegativeAmount,
                CourseErrors.TooManyDecimals,
                CourseErrors.InvalidCurrency,
                CourseErrors.LessonTitleRequired,
                CourseErrors.InvalidDuration,
                CourseErrors.DurationNotWholeSeconds,
            ],
            error => Assert.Equal(ErrorType.Validation, error.Type));
    }

    [Fact]
    public void Invalid_state_transitions_are_conflict_errors()
    {
        Assert.All(
            [CourseErrors.NotDraft, CourseErrors.NoLessons, CourseErrors.NotPublished, CourseErrors.Archived],
            error => Assert.Equal(ErrorType.Conflict, error.Type));
    }

    [Fact]
    public void Acting_on_someone_elses_course_is_a_forbidden_error()
    {
        Assert.Equal(ErrorType.Forbidden, CourseErrors.NotOwner.Type);
        Assert.Equal("catalog.course.not_owner", CourseErrors.NotOwner.Code);
    }

    [Fact]
    public void A_missing_course_is_a_not_found_error()
    {
        Assert.Equal(ErrorType.NotFound, CourseErrors.NotFound.Type);
        Assert.Equal("catalog.course.not_found", CourseErrors.NotFound.Code);
    }
}
