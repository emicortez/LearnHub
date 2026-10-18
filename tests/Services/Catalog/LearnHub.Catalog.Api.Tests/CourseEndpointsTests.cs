using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnHub.Catalog.Api.Tests;

public sealed class CourseEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_course_returns_201_with_a_location_that_resolves_to_the_draft_course()
    {
        var client = factory.CreateClient();
        var instructorId = Guid.NewGuid();
        var request = new { instructorId, title = "  Clean Architecture in .NET  ", price = 49.99m, currency = "usd" };

        var created = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var createdBody = await ReadJsonAsync(created);
        var id = createdBody.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);

        var fetched = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var course = await ReadJsonAsync(fetched);
        Assert.Equal(id, course.GetProperty("id").GetGuid());
        Assert.Equal(instructorId, course.GetProperty("instructorId").GetGuid());
        Assert.Equal("Clean Architecture in .NET", course.GetProperty("title").GetString());
        Assert.Equal("clean-architecture-in-net", course.GetProperty("slug").GetString());
        Assert.Equal(49.99m, course.GetProperty("price").GetDecimal());
        Assert.Equal("USD", course.GetProperty("currency").GetString());
        Assert.Equal("Draft", course.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("", "USD", "catalog.course.title_required")]
    [InlineData("   ", "USD", "catalog.course.title_required")]
    [InlineData("Domain-Driven Design", "dollars", "catalog.money.invalid_currency")]
    [InlineData("Domain-Driven Design", "U5D", "catalog.money.invalid_currency")]
    public async Task Create_course_with_invalid_data_returns_400_with_the_error_code(
        string title, string currency, string expectedCode)
    {
        var client = factory.CreateClient();
        var request = new { instructorId = Guid.NewGuid(), title, price = 10m, currency };

        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal(expectedCode, problem.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Create_course_with_an_empty_instructor_returns_400_instead_of_failing()
    {
        var client = factory.CreateClient();
        var request = new { instructorId = Guid.Empty, title = "Domain-Driven Design", price = 10m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("catalog.course.instructor_required", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("""{ "title": "Docker", "price": 10, "currency": "USD" }""", "InstructorId")]
    [InlineData("""{ "instructorId": "0198a0b4-0000-7000-8000-000000000001", "title": "Docker", "currency": "USD" }""", "Price")]
    public async Task Create_course_with_a_missing_required_field_returns_400_listing_the_field(
        string json, string expectedField)
    {
        var client = factory.CreateClient();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/courses", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.True(
            problem.GetProperty("errors").TryGetProperty(expectedField, out _),
            $"Expected a validation error for {expectedField}, got: {problem}");
    }

    [Fact]
    public async Task Create_course_with_an_explicit_zero_price_creates_a_free_course()
    {
        var client = factory.CreateClient();
        var request = new { instructorId = Guid.NewGuid(), title = "Git for beginners", price = 0m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/courses", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("/courses/0198a0b4-0000-7000-8000-000000000000")]
    [InlineData("/courses/not-a-guid")]
    public async Task Get_course_that_does_not_exist_returns_404(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
