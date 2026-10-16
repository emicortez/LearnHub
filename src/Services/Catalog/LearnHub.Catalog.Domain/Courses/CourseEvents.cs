using LearnHub.SharedKernel;

namespace LearnHub.Catalog.Domain.Courses;

/// <summary>
/// Raised when a course is published, making it available to learners.
/// </summary>
public sealed record CoursePublished(Guid CourseId, string Title, string Slug, Money Price) : IDomainEvent;

/// <summary>
/// Raised when the price of a published course changes.
/// </summary>
public sealed record CoursePriceChanged(Guid CourseId, Money OldPrice, Money NewPrice) : IDomainEvent;

/// <summary>
/// Raised when a course is archived, removing it from new enrollments.
/// </summary>
public sealed record CourseArchived(Guid CourseId) : IDomainEvent;
