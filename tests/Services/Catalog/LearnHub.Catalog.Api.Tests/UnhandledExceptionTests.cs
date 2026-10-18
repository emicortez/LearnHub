using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Domain.Courses;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearnHub.Catalog.Api.Tests;

public sealed class UnhandledExceptionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SecretMessage = "secret connection string xyz";

    [Fact]
    public async Task Unhandled_exception_returns_a_generic_500_problem_without_the_exception_message()
    {
        // Production, so a Development-only error page cannot be what produces the body.
        var client = factory
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Production")
                .ConfigureTestServices(services =>
                    services.Replace(ServiceDescriptor.Singleton<ICourseRepository, ThrowingCourseRepository>())))
            .CreateClient();

        var response = await client.GetAsync($"/courses/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("secret connection string", body, StringComparison.OrdinalIgnoreCase);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("title").GetString());
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    private sealed class ThrowingCourseRepository : ICourseRepository
    {
        public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SecretMessage);

        public Task<IReadOnlyList<Course>> ListAsync(Guid? afterId, int take, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(SecretMessage);

        public void Add(Course course) => throw new InvalidOperationException(SecretMessage);
    }
}
