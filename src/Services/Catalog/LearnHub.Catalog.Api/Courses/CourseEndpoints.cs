using System.Security.Claims;

using LearnHub.Catalog.Api.Authentication;
using LearnHub.Catalog.Api.Caching;
using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Api.Idempotency;
using LearnHub.Catalog.Api.RateLimiting;
using LearnHub.Catalog.Application.Courses;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Catalog.Api.Courses;

public static class CourseEndpoints
{
    private const string GetCourseRoute = "GetCourse";

    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/courses").WithTags("Courses");

        // ProblemHttpResult picks its status at runtime, so error responses are declared explicitly
        // for the OpenAPI document; typed results such as Ok<T> and NotFound document themselves.
        // Writes need a signed-in instructor (401 without a valid token, 403 for anyone else). Reads need no token: it only
        // adds the reader's own unpublished courses. The output cache skips a request with one, so they are never shared.
        group.MapPost("/", CreateCourseAsync)
            .WithSummary("Create a draft course")
            .RequireAuthorization(CatalogPolicies.Instructor)
            .RequireRateLimiting(RateLimitingServiceCollectionExtensions.WritesPolicy)
            .AddEndpointFilter<IdempotencyFilter<CreateCourseRequest>>()
            // Inside the idempotency filter, so a replay does not evict: it is not a new write, and the original
            // request already evicted. Evicting again would be harmless, only a wasted cache refill.
            .AddEndpointFilter<EvictCoursesCacheFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/", ListCoursesAsync)
            .WithSummary("List published courses and your own, one page at a time")
            .CacheOutput(OutputCachingServiceCollectionExtensions.CoursesPolicy)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetCourseAsync)
            .WithName(GetCourseRoute)
            .WithSummary("Get a published course, or one of your own, by id")
            .ReturnsETag()
            .CacheOutput(OutputCachingServiceCollectionExtensions.CoursesPolicy);

        group.MapPost("/{id:guid}/publish", PublishCourseAsync)
            .WithSummary("Publish a draft course")
            .RequiresIfMatch()
            .RequireAuthorization(CatalogPolicies.Instructor)
            .RequireRateLimiting(RateLimitingServiceCollectionExtensions.WritesPolicy)
            .AddEndpointFilter<EvictCoursesCacheFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return endpoints;
    }

    private static async Task<Results<CreatedAtRoute<CreatedCourseResponse>, ProblemHttpResult>> CreateCourseAsync(
        CreateCourseRequest request,
        ClaimsPrincipal user,
        CreateCourseHandler handler,
        CancellationToken cancellationToken)
    {
        // Required fields are guaranteed by validation before the handler runs. The instructor is whoever the token
        // says signed in, never a value from the body: the client cannot choose who it is.
        var command = new CreateCourse(user.GetUserId(), request.Title, request.Price!.Value, request.Currency);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new CreatedCourseResponse(result.Value), GetCourseRoute, new { id = result.Value })
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<CoursePageResponse>, ProblemHttpResult>> ListCoursesAsync(
        [AsParameters] ListCoursesRequest request,
        ClaimsPrincipal user,
        ListCoursesHandler handler,
        CancellationToken cancellationToken)
    {
        Guid? after = null;
        // Empty is no cursor: the output cache cannot tell the two apart, so a token must not change the answer.
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!CourseCursor.TryDecode(request.Cursor, out after))
            {
                return CourseCursor.Invalid.ToProblem();
            }
        }

        var page = await handler.HandleAsync(new ListCourses(request.Limit, after, user.FindUserId()), cancellationToken);

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
        ClaimsPrincipal user,
        HttpResponse response,
        GetCourseHandler handler,
        CancellationToken cancellationToken)
    {
        var course = await handler.HandleAsync(new GetCourse(id, user.FindUserId()), cancellationToken);

        if (course is null)
        {
            return TypedResults.NotFound();
        }

        // Set on the response, so the output cache stores it with the body it describes.
        response.Headers.ETag = CourseETag.Format(course.Version);

        return TypedResults.Ok(new CourseResponse(
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
        [FromHeader(Name = "If-Match")] string? ifMatch,
        ClaimsPrincipal user,
        PublishCourseHandler handler,
        CancellationToken cancellationToken)
    {
        if (CourseETag.IsMissing(ifMatch))
        {
            return CourseETag.Required.ToProblem();
        }

        // The policy proves the caller is an instructor; whether this course is theirs is the use case's decision.
        var command = new PublishCourse(id, user.GetUserId(), CourseETag.ParseVersion(ifMatch));

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.ToProblem();
    }
}
