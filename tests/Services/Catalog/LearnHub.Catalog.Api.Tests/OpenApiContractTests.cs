using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearnHub.Catalog.Api.Tests;

// Contract test: the OpenAPI document is what the frontend client is generated from,
// so every endpoint must document its success and error responses.
public sealed class OpenApiContractTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    [Theory]
    [InlineData("/courses", "post", "201")]
    [InlineData("/courses", "post", "400")]
    [InlineData("/courses", "post", "409")]
    [InlineData("/courses", "post", "422")]
    [InlineData("/courses", "post", "429")]
    [InlineData("/courses", "get", "200")]
    [InlineData("/courses", "get", "400")]
    [InlineData("/courses/{id}", "get", "200")]
    [InlineData("/courses/{id}", "get", "404")]
    [InlineData("/courses/{id}/publish", "post", "204")]
    [InlineData("/courses/{id}/publish", "post", "404")]
    [InlineData("/courses/{id}/publish", "post", "409")]
    [InlineData("/courses/{id}/publish", "post", "429")]
    public async Task OpenApi_document_describes_each_endpoint_response(string path, string method, string status)
    {
        var document = await GetDocumentAsync();

        var responses = document.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("responses");

        Assert.True(responses.TryGetProperty(status, out _), $"{method.ToUpperInvariant()} {path} does not document {status}.");
    }

    [Theory]
    [InlineData("/courses", "post")]
    [InlineData("/courses", "get")]
    [InlineData("/courses/{id}", "get")]
    [InlineData("/courses/{id}/publish", "post")]
    public async Task OpenApi_document_has_a_summary_for_each_endpoint(string path, string method)
    {
        var document = await GetDocumentAsync();

        var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);

        Assert.True(operation.TryGetProperty("summary", out var summary) && summary.GetString()?.Length > 0);
    }

    private async Task<JsonElement> GetDocumentAsync()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
