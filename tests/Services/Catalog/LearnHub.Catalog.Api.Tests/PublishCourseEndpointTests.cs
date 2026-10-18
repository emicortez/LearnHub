using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnHub.Catalog.Api.Tests;

public sealed class PublishCourseEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Publish_course_that_does_not_exist_returns_404_with_the_error_code()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/courses/{Guid.NewGuid()}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.course.not_found", problem.GetProperty("code").GetString());
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Publish_course_without_lessons_returns_409_with_the_error_code()
    {
        var client = factory.CreateClient();
        var request = new { instructorId = Guid.NewGuid(), title = "Event Sourcing", price = 25m, currency = "USD" };
        var created = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();

        var response = await client.PostAsync(
            $"/courses/{id}/publish", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.course.no_lessons", problem.GetProperty("code").GetString());
        Assert.Equal("Conflict", problem.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
