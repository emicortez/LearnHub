using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Creates a draft course for an instructor.
/// </summary>
public sealed record CreateCourse(Guid InstructorId, string? Title, decimal Price, string? Currency);

/// <summary>
/// Handles <see cref="CreateCourse"/>: builds the value objects, creates the aggregate and adds it to the repository.
/// Plain class for now; Wolverine takes over handler dispatch when messaging arrives.
/// </summary>
public sealed class CreateCourseHandler(ICourseRepository courses)
{
    public Task<Result<Guid>> HandleAsync(CreateCourse command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        // Course.Create treats an empty instructor as a programming error and throws;
        // here it is client input, so it becomes an expected failure instead of a 500.
        if (command.InstructorId == Guid.Empty)
        {
            return Task.FromResult(Result.Failure<Guid>(CourseErrors.InstructorRequired));
        }

        var title = CourseTitle.Create(command.Title);
        if (title.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(title.Error));
        }

        var price = Money.Create(command.Price, command.Currency);
        if (price.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(price.Error));
        }

        var course = Course.Create(command.InstructorId, title.Value, price.Value);
        courses.Add(course);

        return Task.FromResult(Result.Success(course.Id));
    }
}
