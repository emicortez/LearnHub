namespace LearnHub.Catalog.Api.Courses;

// HTTP contracts of the course endpoints. They are owned by the Api, so domain objects never leak to clients.

public sealed record CreateCourseRequest(Guid InstructorId, string? Title, decimal Price, string? Currency);

public sealed record CreatedCourseResponse(Guid Id);

public sealed record CourseResponse(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Slug,
    decimal Price,
    string Currency,
    string Status);
