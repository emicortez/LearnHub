using Microsoft.OpenApi;

namespace LearnHub.Catalog.Api.Courses;

/// <summary>
/// What the OpenAPI document cannot infer about the version of a course. The endpoints read If-Match as optional to
/// answer 428 themselves, and they set the ETag on the response, which leaves no trace in their signature.
/// </summary>
internal static class CourseOpenApi
{
    public static TBuilder RequiresIfMatch<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var ifMatch = operation.Parameters!.OfType<OpenApiParameter>().Single(parameter => parameter.Name == "If-Match");
            ifMatch.Required = true;

            return Task.CompletedTask;
        });

    public static TBuilder ReturnsETag<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            var ok = (OpenApiResponse)operation.Responses!["200"];
            ok.Headers = new Dictionary<string, IOpenApiHeader>
            {
                ["ETag"] = new OpenApiHeader
                {
                    Description = "The version of the course. Send it back in If-Match to change it.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                },
            };

            return Task.CompletedTask;
        });
}
