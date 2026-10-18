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

        return endpoints;
    }

    private static async Task<Results<CreatedAtRoute<CreatedCourseResponse>, ProblemHttpResult>> CreateCourseAsync(
        CreateCourseRequest request,
        CreateCourseHandler handler,
        CancellationToken cancellationToken)
    {
        // instructorId comes from the body until authentication lands; then it is read from the JWT 'sub' claim.
        var command = new CreateCourse(request.InstructorId, request.Title, request.Price, request.Currency);

        var result = await handler.HandleAsync(command, cancellationToken);

        // Every business error maps to 400 for now; a richer error-to-status mapping comes later.
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new CreatedCourseResponse(result.Value), GetCourseRoute, new { id = result.Value })
            : TypedResults.Problem(title: result.Error.Code, detail: result.Error.Description, statusCode: StatusCodes.Status400BadRequest);
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
}
