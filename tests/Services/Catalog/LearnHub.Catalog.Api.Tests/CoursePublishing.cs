using System.Net;

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
}
