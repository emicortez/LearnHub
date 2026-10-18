using LearnHub.Catalog.Api.Courses;
using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Api.Idempotency;
using LearnHub.Catalog.Application;
using LearnHub.Catalog.Infrastructure;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

builder.Services.AddIdempotency();

builder.Services
    .AddCatalogApplication()
    .AddCatalogInfrastructure();

var app = builder.Build();

// First, so it catches exceptions thrown anywhere later in the pipeline.
app.UseExceptionHandler();
// Gives empty error responses (such as an unmatched route's 404) a Problem Details body.
app.UseStatusCodePages();

// The document and its UI are for local development only; the frontend client is generated at build time.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapCourseEndpoints();

app.Run();
