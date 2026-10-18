using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Api.Idempotency;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

// The fixture is shared, so every test uses its own key and a unique course title.
public sealed class IdempotencyKeyTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private const string HeaderName = "Idempotency-Key";

    [Fact]
    public async Task A_retry_with_the_same_key_replays_the_response_and_creates_only_one_course()
    {
        var client = factory.CreateClient();
        var key = NewKey();
        var request = NewRequest();

        var first = await PostAsync(client, request, key);
        var second = await PostAsync(client, request, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotNull(first.Headers.Location);
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.Equal(
            await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.False(first.Headers.Contains("Idempotent-Replayed"));
        Assert.True(second.Headers.TryGetValues("Idempotent-Replayed", out var replayed));
        Assert.Equal("true", Assert.Single(replayed));
        Assert.Equal(1, await CountCoursesTitledAsync(client, request.Title));
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_body_returns_422()
    {
        var client = factory.CreateClient();
        var key = NewKey();

        var first = await PostAsync(client, NewRequest(), key);
        var second = await PostAsync(client, NewRequest(), key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await AssertProblemAsync(second, HttpStatusCode.UnprocessableEntity, "catalog.idempotency_key.reused");
    }

    [Fact]
    public async Task A_key_still_in_flight_returns_409()
    {
        var client = factory.CreateClient();
        var key = NewKey();
        // Seeding the store directly makes the in-flight state deterministic, with no concurrent requests.
        var store = factory.Services.GetRequiredService<IIdempotencyStore>();
        await store.TryBeginAsync(key, "another request", TestContext.Current.CancellationToken);

        var response = await PostAsync(client, NewRequest(), key);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "catalog.idempotency_key.in_progress");
    }

    [Fact]
    public async Task Without_the_header_every_post_creates_a_course()
    {
        var client = factory.CreateClient();
        var request = NewRequest();

        var first = await PostAsync(client, request, key: null);
        var second = await PostAsync(client, request, key: null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual(first.Headers.Location, second.Headers.Location);
        Assert.False(second.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(2, await CountCoursesTitledAsync(client, request.Title));
    }

    public static TheoryData<string> InvalidKeys => ["   ", new string('k', 256)];

    [Theory]
    [MemberData(nameof(InvalidKeys))]
    public async Task An_invalid_key_returns_400(string key)
    {
        var client = factory.CreateClient();

        var response = await PostAsync(client, NewRequest(), key);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "catalog.idempotency_key.invalid");
    }

    [Fact]
    public async Task An_empty_key_returns_400()
    {
        // The test client drops a header with an empty value, so the request is built on the server side instead.
        // That path does not flag the request as having a body, hence the explicit body detection feature.
        var body = JsonSerializer.SerializeToUtf8Bytes(NewRequest(), JsonSerializerOptions.Web);

        var context = await factory.Server.SendAsync(
            http =>
            {
                http.Request.Method = HttpMethods.Post;
                http.Request.Path = "/courses";
                http.Request.ContentType = "application/json";
                http.Request.Headers[HeaderName] = string.Empty;
                http.Request.ContentLength = body.Length;
                http.Request.Body = new MemoryStream(body);
                http.Features.Set<IHttpRequestBodyDetectionFeature>(new HasBody());
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(
            context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("catalog.idempotency_key.invalid", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_failed_request_releases_the_key_so_the_corrected_request_succeeds()
    {
        var client = factory.CreateClient();
        var key = NewKey();

        var failed = await PostAsync(client, NewRequest() with { Title = "" }, key);
        var corrected = await PostAsync(client, NewRequest(), key);

        Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
        Assert.Equal(HttpStatusCode.Created, corrected.StatusCode);
        Assert.False(corrected.Headers.Contains("Idempotent-Replayed"));
    }

    private static string NewKey() => Guid.NewGuid().ToString();

    private static CourseRequest NewRequest() => new(Guid.NewGuid(), $"Idempotency {Guid.NewGuid():N}", 10m, "USD");

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, CourseRequest request, string? key)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/courses") { Content = JsonContent.Create(request) };
        if (key is not null)
        {
            message.Headers.TryAddWithoutValidation(HeaderName, key);
        }

        return await client.SendAsync(message, TestContext.Current.CancellationToken);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
    }

    // Pages through the whole list, because the shared fixture holds courses created by other tests.
    private static async Task<int> CountCoursesTitledAsync(HttpClient client, string title)
    {
        var count = 0;
        string? cursor = null;
        for (var page = 0; page < 1000; page++)
        {
            var path = cursor is null ? "/courses?limit=100" : $"/courses?limit=100&cursor={Uri.EscapeDataString(cursor)}";
            var body = await client.GetFromJsonAsync<JsonElement>(path, TestContext.Current.CancellationToken);

            count += body.GetProperty("items").EnumerateArray().Count(item => item.GetProperty("title").GetString() == title);
            var next = body.GetProperty("nextCursor");
            if (next.ValueKind == JsonValueKind.Null)
            {
                return count;
            }

            cursor = next.GetString();
        }

        Assert.Fail("Paging never reached a page without a next cursor.");
        return count;
    }

    private sealed class HasBody : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }

    private sealed record CourseRequest(Guid InstructorId, string Title, decimal Price, string Currency);
}
