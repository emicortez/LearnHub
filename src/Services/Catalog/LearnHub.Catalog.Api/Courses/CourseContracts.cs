using System.ComponentModel.DataAnnotations;

namespace LearnHub.Catalog.Api.Courses;

// HTTP contracts of the course endpoints. They are owned by the Api, so domain objects never leak to clients.

// Value-type fields are nullable so a missing field is rejected instead of silently becoming 0 or Guid.Empty.
// Business rules (title length, currency format) stay in the domain and are not duplicated here.
public sealed record CreateCourseRequest(
    [property: Required] Guid? InstructorId,
    string? Title,
    [property: Required] decimal? Price,
    string? Currency);

public sealed record CreatedCourseResponse(Guid Id);

// Bound from the query string with [AsParameters]; the range is checked by AddValidation() before the handler runs.
public sealed record ListCoursesRequest(
    [property: Range(1, 100)] int Limit = 20,
    string? Cursor = null);

public sealed record CourseSummaryResponse(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    string Status);

// NextCursor is null on the last page.
public sealed record CoursePageResponse(IReadOnlyList<CourseSummaryResponse> Items, string? NextCursor);

public sealed record CourseResponse(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    string Status);
