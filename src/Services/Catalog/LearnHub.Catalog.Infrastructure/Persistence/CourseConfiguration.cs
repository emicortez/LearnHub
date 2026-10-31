using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearnHub.Catalog.Infrastructure.Persistence;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(course => course.Id);

        // The domain creates its ids, so EF Core must not treat a course as new because of its key.
        builder.Property(course => course.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(course => course.InstructorId)
            .HasColumnName("instructor_id");

        builder.Property(course => course.Title)
            .HasColumnName("title")
            .HasConversion(title => title.Value, value => CourseTitle.Create(value).Value);

        builder.Property(course => course.Slug)
            .HasColumnName("slug");

        builder.ComplexProperty(course => course.Price, price =>
        {
            price.Property(money => money.Amount).HasColumnName("price_amount");
            price.Property(money => money.Currency).HasColumnName("price_currency");
        });

        builder.Property(course => course.Status)
            .HasColumnName("status")
            .HasConversion(new CourseStatusConverter());

        // Dispatched after saving, never stored.
        builder.Ignore(course => course.DomainEvents);

        builder.Ignore(course => course.TotalDuration);

        builder.HasMany(course => course.Lessons)
            .WithOne()
            .HasForeignKey(LessonConfiguration.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF Core fills the private list. Every query loads it, because the aggregate's rules need all its lessons.
        builder.Navigation(course => course.Lessons)
            .HasField("_lessons")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }

    private sealed class CourseStatusConverter()
        : ValueConverter<CourseStatus, string>(status => ToColumn(status), value => FromColumn(value))
    {
        private static string ToColumn(CourseStatus status) => status switch
        {
            CourseStatus.Draft => "draft",
            CourseStatus.Published => "published",
            CourseStatus.Archived => "archived",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown course status."),
        };

        private static CourseStatus FromColumn(string value) => value switch
        {
            "draft" => CourseStatus.Draft,
            "published" => CourseStatus.Published,
            "archived" => CourseStatus.Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown course status."),
        };
    }
}
