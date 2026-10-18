using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Api.Idempotency;
using LearnHub.Catalog.Application.Courses;

using Microsoft.AspNetCore.Http.HttpResults;

namespace LearnHub.Catalog.Api.Courses;

public static class CourseEndpoints
{
    private const string GetCourseRoute = "GetCourse";

    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/courses").WithTags("Courses");

        // ProblemHttpResult picks its status at runtime, so error responses are declared explicitly
        // for the OpenAPI document; typed results such as Ok<T> and NotFound document themselves.
        group.MapPost("/", CreateCourseAsync)
            .WithSummary("Create a draft course")
            .AddEndpointFilter<IdempotencyFilter<CreateCourseRequest>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", ListCoursesAsync)
            .WithSummary("List courses one page at a time")
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetCourseAsync)
            .WithName(GetCourseRoute)
            .WithSummary("Get a course by id");

        group.MapPost("/{id:guid}/publish", PublishCourseAsync)
            .WithSummary("Publish a draft course")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<Results<CreatedAtRoute<CreatedCourseResponse>, ProblemHttpResult>> CreateCourseAsync(
        CreateCourseRequest request,
        CreateCourseHandler handler,
        CancellationToken cancellationToken)
    {
        // Required fields are guaranteed by validation before the handler runs.
        // instructorId comes from the body until authentication lands; then it is read from the JWT 'sub' claim.
        var command = new CreateCourse(request.InstructorId!.Value, request.Title, request.Price!.Value, request.Currency);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new CreatedCourseResponse(result.Value), GetCourseRoute, new { id = result.Value })
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<CoursePageResponse>, ProblemHttpResult>> ListCoursesAsync(
        [AsParameters] ListCoursesRequest request,
        ListCoursesHandler handler,
        CancellationToken cancellationToken)
    {
        Guid? after = null;
        if (request.Cursor is not null)
        {
            if (!CourseCursor.TryDecode(request.Cursor, out after))
            {
                return CourseCursor.Invalid.ToProblem();
            }
        }

        var page = await handler.HandleAsync(new ListCourses(request.Limit, after), cancellationToken);

        var items = page.Items
            .Select(course => new CourseSummaryResponse(
                course.Id, course.Title, course.Slug, course.Price, course.Currency, course.Status))
            .ToList();

        return TypedResults.Ok(new CoursePageResponse(
            items,
            page.NextAfter is { } nextAfter ? CourseCursor.Encode(nextAfter) : null));
    }

    private static async Task<Results<Ok<CourseResponse>, NotFound>> GetCourseAsync(
        Guid id,
        GetCourseHandler handler,
        CancellationToken cancellationToken)
    {
        var course = await handler.HandleAsync(new GetCourse(id), cancellationToken);

        return course is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new CourseResponse(
                course.Id,
                course.InstructorId,
                course.Title,
                course.Slug,
                course.Price,
                course.Currency,
                course.Status));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> PublishCourseAsync(
        Guid id,
        PublishCourseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PublishCourse(id), cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.ToProblem();
    }
}
