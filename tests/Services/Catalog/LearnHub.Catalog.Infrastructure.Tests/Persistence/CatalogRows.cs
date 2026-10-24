using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// Inserts valid Catalog rows with plain SQL; each test overrides only the column it wants to break.
/// Defaults are unique per call, so tests share one database without stepping on each other.
/// </summary>
public static class CatalogRows
{
    public static async Task<Guid> InsertCourseAsync(
        NpgsqlDataSource dataSource,
        Guid? id = null,
        Guid? instructorId = null,
        string title = "Clean Architecture in .NET",
        string? slug = null,
        decimal priceAmount = 49.99m,
        string priceCurrency = "USD",
        string? status = null)
    {
        var courseId = id ?? Guid.CreateVersion7();

        // A null status leaves the column out, so the default applies.
        var sql = status is null
            ? """
              INSERT INTO courses (id, instructor_id, title, slug, price_amount, price_currency)
              VALUES ($1, $2, $3, $4, $5, $6)
              """
            : """
              INSERT INTO courses (id, instructor_id, title, slug, price_amount, price_currency, status)
              VALUES ($1, $2, $3, $4, $5, $6, $7)
              """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(courseId);
        command.Parameters.AddWithValue(instructorId ?? Guid.CreateVersion7());
        command.Parameters.AddWithValue(title);
        command.Parameters.AddWithValue(slug ?? $"course-{courseId:N}");
        command.Parameters.AddWithValue(priceAmount);
        command.Parameters.AddWithValue(priceCurrency);
        if (status is not null)
        {
            command.Parameters.AddWithValue(status);
        }

        await command.ExecuteNonQueryAsync();
        return courseId;
    }

    public static async Task<Guid> InsertLessonAsync(
        NpgsqlDataSource dataSource,
        Guid courseId,
        int position,
        string title = "Dependency rule",
        int durationSeconds = 600)
    {
        var lessonId = Guid.CreateVersion7();

        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO lessons (id, course_id, title, duration_seconds, position)
            VALUES ($1, $2, $3, $4, $5)
            """);
        command.Parameters.AddWithValue(lessonId);
        command.Parameters.AddWithValue(courseId);
        command.Parameters.AddWithValue(title);
        command.Parameters.AddWithValue(durationSeconds);
        command.Parameters.AddWithValue(position);

        await command.ExecuteNonQueryAsync();
        return lessonId;
    }

    public static async Task<long> CountAsync(NpgsqlDataSource dataSource, string sql, Guid id)
    {
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(id);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
