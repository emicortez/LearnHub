using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

public sealed class CoursePersistenceTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task Created_course_is_stored_in_the_database()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var request = new { title = "Stored Course", price = 25m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/courses", request, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Courses
            .SingleAsync(course => course.Id == id, cancellationToken);
        Assert.Equal("Stored Course", stored.Title.Value);
    }

    [Fact]
    public async Task Create_with_a_taken_slug_returns_409_with_the_error_code_and_no_database_details()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var other = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var first = await client.PostAsJsonAsync(
            "/courses", new { title = "Taken Slug", price = 25m, currency = "USD" }, cancellationToken);
        var second = await other.PostAsJsonAsync(
            "/courses", new { title = "taken slug!", price = 30m, currency = "USD" }, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        var body = await second.Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal("catalog.course.slug_taken", JsonDocument.Parse(body).RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("uq_courses_slug", body, StringComparison.Ordinal);
        Assert.DoesNotContain("23505", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Two_simultaneous_creates_with_one_slug_store_one_course_and_the_other_gets_409()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var request = new { title = "Simultaneous Course", price = 25m, currency = "USD" };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/courses", request, cancellationToken),
            client.PostAsJsonAsync("/courses", request, cancellationToken));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            responses.Select(response => response.StatusCode).Order());
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Courses
            .CountAsync(course => course.Slug == "simultaneous-course", cancellationToken));
    }

    [Fact]
    public async Task Create_with_the_highest_price_the_schema_stores_returns_201()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var request = new { title = "Highest Price", price = 9999999999.99m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_a_price_the_schema_cannot_store_returns_400_with_the_error_code()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var request = new { title = "Too Expensive", price = 10000000000m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("catalog.money.amount_too_large", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
