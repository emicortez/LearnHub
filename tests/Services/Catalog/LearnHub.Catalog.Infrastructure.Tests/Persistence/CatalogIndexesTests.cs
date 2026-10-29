using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// The exact set of Catalog indexes: the ones the constraints create plus the ones schema 002 adds for named queries.
/// A missing, changed or extra index fails here. Whether the planner uses them is measured with EXPLAIN separately.
/// </summary>
public sealed class CatalogIndexesTests(CatalogDatabaseFixture database)
{
    private readonly NpgsqlDataSource _db = database.DataSource;

    [Fact]
    public async Task Catalog_has_exactly_the_constraint_indexes_and_the_query_indexes()
    {
        Assert.Equal(
            [
                ("ix_courses_instructor_title_slug",
                    "CREATE INDEX ix_courses_instructor_title_slug ON public.courses USING btree (instructor_id, title, slug)"),
                ("ix_courses_published_title_slug",
                    "CREATE INDEX ix_courses_published_title_slug ON public.courses USING btree (title, slug) WHERE (status = 'published'::text)"),
                ("pk_courses", "CREATE UNIQUE INDEX pk_courses ON public.courses USING btree (id)"),
                ("pk_lessons", "CREATE UNIQUE INDEX pk_lessons ON public.lessons USING btree (id)"),
                ("uq_courses_slug", "CREATE UNIQUE INDEX uq_courses_slug ON public.courses USING btree (slug)"),
                // Starts with course_id, so it also serves fk_lessons_course and its cascade: no separate index.
                ("uq_lessons_course_position",
                    "CREATE UNIQUE INDEX uq_lessons_course_position ON public.lessons USING btree (course_id, \"position\")"),
            ],
            await IndexesAsync());
    }

    private async Task<List<(string Name, string Definition)>> IndexesAsync()
    {
        await using var command = _db.CreateCommand(
            """
            SELECT indexname, indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename IN ('courses', 'lessons')
            ORDER BY indexname COLLATE "C"
            """);

        await using var row = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var indexes = new List<(string, string)>();
        while (await row.ReadAsync(TestContext.Current.CancellationToken))
        {
            indexes.Add((row.GetString(0), row.GetString(1)));
        }

        return indexes;
    }
}
