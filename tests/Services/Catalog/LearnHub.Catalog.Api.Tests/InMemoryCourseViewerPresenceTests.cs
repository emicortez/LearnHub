using LearnHub.Catalog.Api.Viewers;

namespace LearnHub.Catalog.Api.Tests;

public sealed class InMemoryCourseViewerPresenceTests
{
    private static readonly Guid CourseA = Guid.NewGuid();
    private static readonly Guid CourseB = Guid.NewGuid();

    [Fact]
    public void The_count_is_the_number_of_distinct_connections_watching_a_course()
    {
        var presence = new InMemoryCourseViewerPresence();

        Assert.Equal(1, presence.Add("one", CourseA));
        Assert.Equal(1, presence.Add("one", CourseA));
        Assert.Equal(2, presence.Add("two", CourseA));
        Assert.Equal(1, presence.Add("two", CourseB));

        Assert.Equal(2, presence.Count(CourseA));
        Assert.Equal(1, presence.Count(CourseB));
    }

    [Fact]
    public void Removing_a_connection_from_a_course_lowers_only_that_course()
    {
        var presence = new InMemoryCourseViewerPresence();
        presence.Add("one", CourseA);
        presence.Add("two", CourseA);
        presence.Add("one", CourseB);

        Assert.Equal(1, presence.Remove("one", CourseA));
        Assert.Equal(1, presence.Remove("one", CourseA));
        Assert.Equal(1, presence.Remove("never-watched", CourseB));

        Assert.Equal(1, presence.Count(CourseA));
        Assert.Equal(1, presence.Count(CourseB));
    }

    [Fact]
    public void Removing_a_connection_forgets_every_course_it_watched_and_returns_their_new_counts()
    {
        var presence = new InMemoryCourseViewerPresence();
        presence.Add("one", CourseA);
        presence.Add("one", CourseB);
        presence.Add("two", CourseA);

        var changed = presence.RemoveConnection("one");

        Assert.Equal(new Dictionary<Guid, int> { [CourseA] = 1, [CourseB] = 0 }, changed);
        Assert.Equal(1, presence.Count(CourseA));
        Assert.Equal(0, presence.Count(CourseB));
        Assert.Empty(presence.RemoveConnection("one"));
    }

    [Fact]
    public async Task Concurrent_adds_and_removes_end_at_the_right_count()
    {
        var presence = new InMemoryCourseViewerPresence();
        const int Connections = 1000;

        // Every connection joins; the even ones also leave, some by course and some by disconnecting.
        await Parallel.ForAsync(0, Connections, TestContext.Current.CancellationToken, (i, _) =>
        {
            var connectionId = $"connection-{i}";
            presence.Add(connectionId, CourseA);
            presence.Add(connectionId, CourseA);
            if (i % 2 == 0)
            {
                if (i % 4 == 0)
                {
                    presence.Remove(connectionId, CourseA);
                }
                else
                {
                    presence.RemoveConnection(connectionId);
                }
            }

            return ValueTask.CompletedTask;
        });

        Assert.Equal(Connections / 2, presence.Count(CourseA));
    }
}
