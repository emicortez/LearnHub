using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LearnHub.Catalog.Application;
using LearnHub.Catalog.Application.Courses;
using LearnHub.Catalog.Infrastructure.Persistence;
using LearnHub.Catalog.Infrastructure.Tests.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Catalog.Api.Tests;

/// <summary>
/// Pins what each public read costs the database, whatever handler or query serves it: one command, and no lessons.
/// </summary>
public sealed class ReadCommandsTests : IAsyncDisposable
{
    private readonly CatalogApiFactory _factory = new();
    private readonly CommandRecorder _recorder = new();
    private readonly WebApplicationFactory<Program> _host;

    public ReadCommandsTests() =>
        _host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<CatalogDbContext>(options => options.AddInterceptors(_recorder))));

    [Fact]
    public async Task Listing_courses_sends_one_command_however_many_courses_and_lessons_the_page_has()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _host.CreateAuthenticatedClient(Guid.NewGuid());
        for (var course = 0; course < 5; course++)
        {
            await CreateCourseWithLessonsAsync(client, $"Course {course}");
        }

        _recorder.Clear();
        var response = await client.GetAsync("/courses?limit=100", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        Assert.Equal(5, page.GetProperty("items").GetArrayLength());
        var command = Assert.Single(_recorder.Commands);
        Assert.DoesNotContain("lessons", command.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Getting_a_course_sends_one_command_without_its_lessons()
    {
        var client = _host.CreateAuthenticatedClient(Guid.NewGuid());
        var id = await CreateCourseWithLessonsAsync(client, "One Course");

        _recorder.Clear();
        var response = await client.GetAsync($"/courses/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var command = Assert.Single(_recorder.Commands);
        Assert.DoesNotContain("lessons", command.Text, StringComparison.Ordinal);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private async Task<Guid> CreateCourseWithLessonsAsync(HttpClient client, string title)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new { title, price = 10m, currency = "USD" };
        var created = await client.PostAsJsonAsync("/courses", request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("id").GetGuid();

        // The API cannot add lessons yet, so they are saved directly.
        await using var scope = _host.Services.CreateAsyncScope();
        var course = await scope.ServiceProvider.GetRequiredService<ICourseRepository>().GetAsync(id, cancellationToken);
        Assert.True(course!.AddLesson("Introduction", TimeSpan.FromMinutes(5)).IsSuccess);
        Assert.True(course.AddLesson("Summary", TimeSpan.FromMinutes(3)).IsSuccess);
        var saved = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        Assert.True(saved.IsSuccess);

        return id;
    }
}
