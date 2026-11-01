using LearnHub.Catalog.Domain.Courses;
using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Application.Courses;

/// <summary>
/// Creates a draft course for an instructor.
/// </summary>
public sealed record CreateCourse(Guid InstructorId, string? Title, decimal Price, string? Currency);

/// <summary>
/// Handles <see cref="CreateCourse"/>: builds the value objects, creates the aggregate, adds it to the repository
/// and commits it. A taken slug comes back from the commit, not from asking first, which two requests could both pass.
/// Plain class for now; Wolverine takes over handler dispatch when messaging arrives.
/// </summary>
public sealed class CreateCourseHandler(ICourseRepository courses, IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> HandleAsync(CreateCourse command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Course.Create treats an empty instructor as a programming error and throws;
        // here it is client input, so it becomes an expected failure instead of a 500.
        if (command.InstructorId == Guid.Empty)
        {
            return CourseErrors.InstructorRequired;
        }

        var title = CourseTitle.Create(command.Title);
        if (title.IsFailure)
        {
            return title.Error;
        }

        var price = Money.Create(command.Price, command.Currency);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var course = Course.Create(command.InstructorId, title.Value, price.Value);
        courses.Add(course);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return saved.IsSuccess ? course.Id : saved.Error;
    }
}
