using System.Net;

using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

internal static class CoursePublishing
{
    /// <summary>
    /// Publishes with the ETag the client reads first, as a client with an up-to-date screen would. A course that
    /// cannot be read gets an arbitrary tag, so the request still reaches the rules under test.
    /// </summary>
    public static async Task<HttpResponseMessage> PublishAsync(this HttpClient client, Guid courseId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = $"/courses/{courseId}";

        // Reads with this client: with an Authorization header it skips the cache, so the tag is the current one.
        var course = await client.GetAsync(path, cancellationToken);
        var etag = course.StatusCode == HttpStatusCode.OK ? course.Headers.ETag!.Tag : "\"1\"";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}/publish");
        request.Headers.TryAddWithoutValidation("If-Match", etag);

        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Makes a draft public, for tests about what anyone can read. The API cannot add lessons yet, so the lesson is
    /// saved directly; the publish goes through the API.
    /// </summary>
    public static async Task PublishWithLessonAsync(this WebApplicationFactory<Program> host, HttpClient owner, Guid courseId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var course = await scope.ServiceProvider.GetRequiredService<ICourseRepository>()
                .GetAsync(courseId, cancellationToken);
            Assert.True(course!.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
            var saved = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            Assert.True(saved.IsSuccess);
        }

        Assert.Equal(HttpStatusCode.NoContent, (await owner.PublishAsync(courseId)).StatusCode);
    }
}
