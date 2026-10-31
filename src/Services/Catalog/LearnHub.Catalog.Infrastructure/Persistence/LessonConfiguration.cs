using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearnHub.Catalog.Infrastructure.Persistence;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public const string CourseId = nameof(CourseId);

    public const string Position = nameof(Position);

    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(lesson => lesson.Id);

        // The domain creates its ids. With a generated key, a new lesson added to a loaded course would count as an
        // existing row, and EF Core would send an UPDATE instead of an INSERT.
        builder.Property(lesson => lesson.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(lesson => lesson.Title)
            .HasColumnName("title");

        builder.Property(lesson => lesson.Duration)
            .HasColumnName("duration_seconds")
            .HasConversion(new DurationConverter());

        // Lesson knows neither of these: they say where it sits in the aggregate, so they stay in the EF Core model.
        builder.Property<Guid>(CourseId)
            .HasColumnName("course_id");

        builder.Property<int>(Position)
            .HasColumnName("position");
    }

    private sealed class DurationConverter()
        : ValueConverter<TimeSpan, int>(duration => ToColumn(duration), seconds => FromColumn(seconds))
    {
        // Checked, so a value past the integer column fails instead of wrapping. The domain allows only whole seconds.
        private static int ToColumn(TimeSpan duration) => checked((int)duration.TotalSeconds);

        private static TimeSpan FromColumn(int seconds) => TimeSpan.FromSeconds(seconds);
    }
}
