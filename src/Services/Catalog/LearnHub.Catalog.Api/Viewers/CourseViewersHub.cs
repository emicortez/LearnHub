using LearnHub.Catalog.Application.Courses;

using Microsoft.AspNetCore.SignalR;

namespace LearnHub.Catalog.Api.Viewers;

/// <summary>
/// Live "people viewing this course" counts. A client calls <see cref="WatchCourse"/> when a course page opens,
/// and every viewer of that course receives <see cref="ICourseViewersClient.ViewerCountChanged"/>.
/// A hub instance lives for one call only, so it keeps no state in fields: presence lives in a singleton store.
/// </summary>
public sealed class CourseViewersHub(GetCourseHandler getCourse, ICourseViewerPresence presence)
    : Hub<ICourseViewersClient>
{
    public async Task WatchCourse(Guid courseId)
    {
        var course = await getCourse.HandleAsync(new GetCourse(courseId), Context.ConnectionAborted);
        if (course is null)
        {
            // Only a HubException's message reaches the client; any other exception is reported as a generic error.
            throw new HubException($"Course {courseId} does not exist.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(courseId), Context.ConnectionAborted);
        var count = presence.Add(Context.ConnectionId, courseId);

        // The whole group, caller included, so the new viewer sees the count without a separate read.
        await Clients.Group(GroupName(courseId)).ViewerCountChanged(courseId, count);
    }

    public async Task StopWatching(Guid courseId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(courseId), Context.ConnectionAborted);
        var count = presence.Remove(Context.ConnectionId, courseId);

        await Clients.Group(GroupName(courseId)).ViewerCountChanged(courseId, count);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR drops the connection from its groups on its own; the presence store must be told explicitly.
        foreach (var (courseId, count) in presence.RemoveConnection(Context.ConnectionId))
        {
            await Clients.Group(GroupName(courseId)).ViewerCountChanged(courseId, count);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static string GroupName(Guid courseId) => $"course:{courseId}";
}
