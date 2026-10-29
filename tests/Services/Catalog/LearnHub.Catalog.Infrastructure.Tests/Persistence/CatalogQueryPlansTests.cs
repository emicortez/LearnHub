using System.Text.Json;

using Npgsql;

namespace LearnHub.Catalog.Infrastructure.Tests.Persistence;

/// <summary>
/// One database of its own with enough rows for the planner to prefer indexes, loaded once for every plan test.
/// With a handful of rows a sequential scan is the cheapest plan, so a plan read there proves nothing.
/// </summary>
public sealed class CatalogPlanDatabase(CatalogDatabaseFixture database) : IAsyncLifetime
{
    public const string PublishedCourseId = "00000000-0000-0000-0000-000000000010";
    public const string InstructorId = "00000000-0000-0000-0000-000000000005";

    private NpgsqlDataSource? _dataSource;

    public NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("The plan database is not loaded yet.");

    public async ValueTask InitializeAsync()
    {
        _dataSource = await database.CreateEmptyDatabaseAsync();

        // 100,000 courses from 1,000 instructors: 10% published, 10% archived, 80% drafts, with repeated titles;
        // five lessons each. ANALYZE afterwards, or the planner keeps estimating an empty table.
        await using var load = _dataSource.CreateCommand(
            """
            INSERT INTO courses (id, instructor_id, title, slug, price_amount, price_currency, status)
            SELECT ('00000000-0000-0000-0000-' || lpad(n::text, 12, '0'))::uuid,
                   ('00000000-0000-0000-0000-' || lpad((n % 1000 + 1)::text, 12, '0'))::uuid,
                   'Course ' || lpad((n % 5000)::text, 5, '0'),
                   'course-' || n,
                   10,
                   'USD',
                   CASE WHEN n % 10 = 0 THEN 'published' WHEN n % 10 = 1 THEN 'archived' ELSE 'draft' END
            FROM generate_series(1, 100000) AS n;

            INSERT INTO lessons (id, course_id, title, duration_seconds, position)
            SELECT gen_random_uuid(), c.id, 'Lesson ' || p, 300, p
            FROM courses AS c
            CROSS JOIN generate_series(1, 5) AS p;

            ANALYZE courses;
            ANALYZE lessons;
            """);
        load.CommandTimeout = 120;
        await load.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }
    }
}

/// <summary>
/// Reads the plan PostgreSQL chooses for each versioned query (EXPLAIN (FORMAT JSON)) and checks which index it uses
/// and which condition bounds it, never the exact shape of the whole tree, which changes between versions.
/// If one of these fails, fix the index or the query, not the test.
/// </summary>
public sealed class CatalogQueryPlansTests(CatalogPlanDatabase plans) : IClassFixture<CatalogPlanDatabase>
{
    private const int PageSize = 20;

    [Fact]
    public async Task First_page_reads_the_published_index_and_limits_before_joining_the_lessons()
    {
        var plan = await PlanAsync("list_published_courses_first_page.sql", PageSize);

        // The page of courses is cut by a Limit over the partial index, and lessons are only looked up for that page.
        var limit = Assert.Single(plan.Descendants(), node => node.Type == "Limit");
        Assert.Contains(limit.Descendants(), node => node.IndexName == "ix_courses_published_title_slug");
        Assert.DoesNotContain(limit.Descendants(), node => node.RelationName == "lessons");
        Assert.Contains(plan.Descendants(), node => node.IndexName == "uq_lessons_course_position");
    }

    [Fact]
    public async Task Next_page_starts_at_the_cursor_inside_the_published_index()
    {
        var plan = await PlanAsync("list_published_courses_next_page.sql", "Course 02500", "course-2500", PageSize);

        var courses = Assert.Single(plan.Descendants(), node => node.IndexName == "ix_courses_published_title_slug");
        // An Index Cond bounds the search: the scan starts at the cursor instead of reading and skipping earlier rows.
        Assert.Contains("ROW(title, slug) > ROW(", courses.IndexCondition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Next_page_still_uses_the_partial_index_with_a_generic_plan()
    {
        // A generic plan does not know the parameter values; the partial index stays usable because the query
        // writes status = 'published' as a literal, not as a parameter.
        var plan = await GenericPlanAsync("list_published_courses_next_page.sql", "'Course 02500', 'course-2500', 20");

        var courses = Assert.Single(plan.Descendants(), node => node.IndexName == "ix_courses_published_title_slug");
        Assert.Contains("ROW(title, slug) > ROW($1, $2)", courses.IndexCondition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Instructor_listing_finds_the_instructors_courses_through_its_index()
    {
        var plan = await PlanAsync("list_instructor_courses.sql", new Guid(CatalogPlanDatabase.InstructorId));

        // An Index Scan or a Bitmap Index Scan: either way the index bounds the search by instructor.
        var courses = Assert.Single(plan.Descendants(), node => node.IndexName == "ix_courses_instructor_title_slug");
        Assert.Contains("instructor_id = ", courses.IndexCondition, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Descendants(), node => node.Type == "Seq Scan");
    }

    [Fact]
    public async Task Outline_reads_one_course_and_its_lessons_through_their_indexes()
    {
        var plan = await PlanAsync("get_published_course_outline.sql", new Guid(CatalogPlanDatabase.PublishedCourseId));

        var lessons = Assert.Single(plan.Descendants(), node => node.IndexName == "uq_lessons_course_position");
        Assert.Contains("course_id = ", lessons.IndexCondition, StringComparison.Ordinal);
        Assert.Contains(plan.Descendants(), node => node.IndexName == "pk_courses");
        Assert.DoesNotContain(plan.Descendants(), node => node.Type == "Seq Scan");
    }

    private async Task<PlanNode> PlanAsync(string file, params object[] parameters)
    {
        await using var command = plans.DataSource.CreateCommand(
            "EXPLAIN (FORMAT JSON) " + await CatalogQueryFiles.ReadAsync(file));
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        return PlanNode.Parse((string)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    private async Task<PlanNode> GenericPlanAsync(string file, string arguments)
    {
        await using var connection = await plans.DataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var batch = new NpgsqlBatch(connection)
        {
            BatchCommands =
            {
                new("SET plan_cache_mode = force_generic_plan"),
                new("PREPARE plan_under_test AS " + await CatalogQueryFiles.ReadAsync(file)),
                new($"EXPLAIN (FORMAT JSON) EXECUTE plan_under_test({arguments})"),
                new("DEALLOCATE plan_under_test"),
                new("RESET plan_cache_mode"),
            },
        };

        await using var reader = await batch.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        return PlanNode.Parse(reader.GetString(0));
    }

    private sealed record PlanNode(
        string Type,
        string? RelationName,
        string? IndexName,
        string IndexCondition,
        IReadOnlyList<PlanNode> Children)
    {
        public static PlanNode Parse(string json)
        {
            using var document = JsonDocument.Parse(json);
            return From(document.RootElement[0].GetProperty("Plan"));
        }

        public IEnumerable<PlanNode> Descendants() =>
            Children.SelectMany(child => child.Descendants()).Prepend(this);

        private static PlanNode From(JsonElement node) => new(
            node.GetProperty("Node Type").GetString()!,
            Text(node, "Relation Name"),
            Text(node, "Index Name"),
            // A Bitmap Heap Scan only rechecks; the bounding condition is on its Bitmap Index Scan child.
            Text(node, "Index Cond") ?? "",
            node.TryGetProperty("Plans", out var children) ? [.. children.EnumerateArray().Select(From)] : []);

        private static string? Text(JsonElement node, string property) =>
            node.TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
