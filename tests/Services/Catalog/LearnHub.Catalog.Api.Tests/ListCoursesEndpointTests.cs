using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnHub.Catalog.Api.Tests;

// The class fixture gives these tests their own in-memory store, but assertions still use
// the ids each test created, so they stay correct whatever else is in the list.
public sealed class ListCoursesEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Paging_with_the_cursor_returns_every_course_exactly_once()
    {
        var client = factory.CreateClient();
        var created = await CreateCoursesAsync(client, 7);

        var pages = await ReadAllPagesAsync(client, "/courses?limit=3");

        Assert.All(pages, page => Assert.InRange(page.Items.Count, 1, 3));
        var listed = pages.SelectMany(page => page.Items).ToList();
        Assert.Equal(listed.Count, listed.Distinct().Count());
        Assert.All(created, id => Assert.Single(listed, listedId => listedId == id));
    }

    [Fact]
    public async Task The_last_page_has_no_next_cursor()
    {
        var client = factory.CreateClient();
        await CreateCoursesAsync(client, 4);

        var pages = await ReadAllPagesAsync(client, "/courses?limit=3");

        // The cursor must stop on the last page with items, not lead the client to an extra empty page.
        Assert.NotEmpty(pages[^1].Items);
        Assert.Null(pages[^1].NextCursor);
        Assert.All(pages[..^1], page => Assert.NotNull(page.NextCursor));
    }

    [Fact]
    public async Task List_items_carry_the_course_summary()
    {
        var client = factory.CreateClient();
        var created = await CreateCoursesAsync(client, 1);

        var pages = await ReadAllPagesAsync(client, "/courses?limit=100");

        var item = pages.SelectMany(page => page.Raw).Single(raw => raw.GetProperty("id").GetGuid() == created[0]);
        Assert.Equal("Course 0", item.GetProperty("title").GetString());
        Assert.Equal("course-0", item.GetProperty("slug").GetString());
        Assert.Equal(10m, item.GetProperty("price").GetDecimal());
        Assert.Equal("USD", item.GetProperty("currency").GetString());
        Assert.Equal("Draft", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Omitting_the_limit_returns_at_most_20_items()
    {
        var client = factory.CreateClient();
        await CreateCoursesAsync(client, 21);

        var response = await client.GetAsync("/courses", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = ToPage(await ReadJsonAsync(response));
        Assert.Equal(20, page.Items.Count);
        Assert.NotNull(page.NextCursor);
    }

    [Fact]
    public async Task An_invalid_cursor_returns_400_with_the_error_code()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/courses?cursor=not-a-cursor!", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.course.invalid_cursor", problem.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task An_out_of_range_limit_returns_400_listing_the_field(int limit)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/courses?limit={limit}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.True(
            problem.TryGetProperty("errors", out var errors) && errors.EnumerateObject().Any(),
            $"Expected a validation error for the limit, got: {problem}");
    }

    private static async Task<List<Guid>> CreateCoursesAsync(HttpClient client, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var request = new { instructorId = Guid.NewGuid(), title = $"Course {i}", price = 10m, currency = "USD" };
            var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add((await ReadJsonAsync(response)).GetProperty("id").GetGuid());
        }

        return ids;
    }

    // Follows nextCursor until it is null; the guard stops a broken cursor from looping forever.
    private static async Task<List<Page>> ReadAllPagesAsync(HttpClient client, string firstPage)
    {
        var pages = new List<Page>();
        var path = firstPage;
        while (pages.Count < 1000)
        {
            var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var page = ToPage(await ReadJsonAsync(response));
            pages.Add(page);
            if (page.NextCursor is null)
            {
                return pages;
            }

            path = $"{firstPage}&cursor={Uri.EscapeDataString(page.NextCursor)}";
        }

        Assert.Fail("Paging never reached a page without a next cursor.");
        return pages;
    }

    private static Page ToPage(JsonElement body)
    {
        var raw = body.GetProperty("items").EnumerateArray().ToList();
        var nextCursor = body.GetProperty("nextCursor");

        return new Page(
            raw,
            raw.Select(item => item.GetProperty("id").GetGuid()).ToList(),
            nextCursor.ValueKind == JsonValueKind.Null ? null : nextCursor.GetString());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private sealed record Page(List<JsonElement> Raw, List<Guid> Items, string? NextCursor);
}
