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
        builder.ToTable("lessons", table =>
        {
            table.HasCheckConstraint("ck_lessons_title_not_blank", "title ~ '[^[:space:]]'");
            table.HasCheckConstraint("ck_lessons_duration_positive", "duration_seconds > 0");
            table.HasCheckConstraint("ck_lessons_position_positive", "position > 0");
        });

        builder.HasKey(lesson => lesson.Id)
            .HasName("pk_lessons");

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

        // Tables have no order, so the aggregate's list order is stored. The migration creates this as a UNIQUE
        // constraint: an alternate key would reject two new lessons in the tracker, before they get their position.
        // It starts with course_id, so it also serves fk_lessons_course, which PostgreSQL does not index by itself.
        builder.HasIndex(CourseId, Position)
            .IsUnique()
            .HasDatabaseName("uq_lessons_course_position");
    }

    private sealed class DurationConverter()
        : ValueConverter<TimeSpan, int>(duration => ToColumn(duration), seconds => FromColumn(seconds))
    {
        // Checked, so a value past the integer column fails instead of wrapping. The domain allows only whole seconds.
        private static int ToColumn(TimeSpan duration) => checked((int)duration.TotalSeconds);

        private static TimeSpan FromColumn(int seconds) => TimeSpan.FromSeconds(seconds);
    }
}
