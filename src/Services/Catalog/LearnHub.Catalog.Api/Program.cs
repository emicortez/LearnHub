using LearnHub.Catalog.Api.Caching;
using LearnHub.Catalog.Api.Courses;
using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Api.Idempotency;
using LearnHub.Catalog.Api.RateLimiting;
using LearnHub.Catalog.Api.Viewers;
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
builder.Services.AddCatalogRateLimiting(builder.Configuration);
builder.Services.AddCatalogOutputCaching();
builder.Services.AddCourseViewers();

builder.Services
    .AddCatalogApplication()
    .AddCatalogInfrastructure();

var app = builder.Build();

// Before everything else, so the rate limiter, logs and generated links see the client's IP and scheme, not the proxy's.
app.UseForwardedHeaders();
// Next, so it catches exceptions thrown anywhere later in the pipeline.
app.UseExceptionHandler();
// Gives empty error responses (such as an unmatched route's 404) a Problem Details body.
app.UseStatusCodePages();
// Partitioned by client IP for now (the forwarded one behind a trusted proxy). With per-user partitions (4b) this
// moves after UseAuthentication.
app.UseRateLimiter();
// After the rate limiter and before the endpoints. By default it skips requests with an Authorization header, so once
// authentication lands (4b) authenticated reads bypass the cache unless a policy opts them in.
app.UseOutputCache();

// The document and its UI are for local development only; the frontend client is generated at build time.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapCourseEndpoints();
app.MapCourseViewers();

app.Run();
