using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Application.Courses;

using Microsoft.AspNetCore.Http.HttpResults;

namespace LearnHub.Catalog.Api.Courses;

public static class CourseEndpoints
{
    private const string GetCourseRoute = "GetCourse";

    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/courses").WithTags("Courses");

        group.MapPost("/", CreateCourseAsync);
        group.MapGet("/{id:guid}", GetCourseAsync).WithName(GetCourseRoute);
        group.MapPost("/{id:guid}/publish", PublishCourseAsync);

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
