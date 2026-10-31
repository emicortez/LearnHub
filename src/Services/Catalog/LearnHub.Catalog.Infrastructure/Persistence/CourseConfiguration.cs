using LearnHub.Catalog.Domain.Courses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearnHub.Catalog.Infrastructure.Persistence;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        // The domain validates first to return useful errors. These constraints keep the stored data valid whatever
        // writes it, and each is named so a violation says which rule failed.
        builder.ToTable("courses", table =>
        {
            table.HasCheckConstraint(
                "ck_courses_instructor_id_not_empty",
                "instructor_id <> '00000000-0000-0000-0000-000000000000'::uuid");

            // "Not blank" means one non-whitespace character: btrim() trims only spaces, so a tab-only title would
            // pass, while the domain's string.Trim() removes every kind of whitespace.
            table.HasCheckConstraint(
                "ck_courses_title_length",
                "title ~ '[^[:space:]]' AND char_length(btrim(title)) <= 120");

            table.HasCheckConstraint("ck_courses_slug_not_blank", "slug ~ '[^[:space:]]'");
            table.HasCheckConstraint("ck_courses_price_range", "price_amount BETWEEN 0 AND 9999999999.99");
            table.HasCheckConstraint("ck_courses_price_scale", "price_amount = round(price_amount, 2)");
            table.HasCheckConstraint("ck_courses_currency", "price_currency::text ~ '^[A-Z]{3}$'");
            table.HasCheckConstraint("ck_courses_status", "status IN ('draft', 'published', 'archived')");
        });

        builder.HasKey(course => course.Id)
            .HasName("pk_courses");

        // The migration creates this as a UNIQUE constraint, which EF Core maps only as an alternate key.
        builder.HasIndex(course => course.Slug)
            .IsUnique()
            .HasDatabaseName("uq_courses_slug");

        // list_published_courses*.sql: only published rows, in the (title, slug) order the keyset cursor compares.
        // The query must repeat status = 'published' literally for the planner to use it.
        builder.HasIndex(course => new { course.Title, course.Slug })
            .HasDatabaseName("ix_courses_published_title_slug")
            .HasFilter("status = 'published'");

        // list_instructor_courses.sql: equality on instructor_id, then its (title, slug) order, for every status.
        builder.HasIndex(course => new { course.InstructorId, course.Title, course.Slug })
            .HasDatabaseName("ix_courses_instructor_title_slug");

        // The domain creates its ids, so EF Core must not treat a course as new because of its key.
        builder.Property(course => course.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // The instructor's Entra oid. No foreign key: identities live in the identity provider (ADR 0009).
        builder.Property(course => course.InstructorId)
            .HasColumnName("instructor_id");

        builder.Property(course => course.Title)
            .HasColumnName("title")
            .HasConversion(title => title.Value, value => CourseTitle.Create(value).Value);

        builder.Property(course => course.Slug)
            .HasColumnName("slug");

        builder.ComplexProperty(course => course.Price, price =>
        {
            // Unconstrained numeric plus the checks above: numeric(12, 2) would silently round 10.129 to 10.13.
            price.Property(money => money.Amount).HasColumnName("price_amount").HasColumnType("numeric");
            price.Property(money => money.Currency).HasColumnName("price_currency").HasColumnType("char(3)");
        });

        builder.Property(course => course.Status)
            .HasColumnName("status")
            .HasConversion(new CourseStatusConverter())
            .HasDefaultValueSql("'draft'")
            .ValueGeneratedNever();

        // Dispatched after saving, never stored.
        builder.Ignore(course => course.DomainEvents);

        builder.Ignore(course => course.TotalDuration);

        builder.HasMany(course => course.Lessons)
            .WithOne()
            .HasForeignKey(LessonConfiguration.CourseId)
            .HasConstraintName("fk_lessons_course")
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
