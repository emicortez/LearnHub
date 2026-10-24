using LearnHub.Catalog.Api.Authentication;
using LearnHub.Catalog.Api.Caching;
using LearnHub.Catalog.Api.Cors;
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

builder.Services.AddCatalogCors(builder.Configuration);
builder.Services.AddCatalogAuthentication();
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
// Before authentication, authorization and the rate limiter: it answers preflights before those gates run, and a 401,
// 403 or 429 still carries the CORS headers an allowed page needs to read why it was rejected.
app.UseCors(CatalogCorsServiceCollectionExtensions.FrontendPolicy);
// Authentication only builds HttpContext.User from a valid token, leaving the request anonymous otherwise;
// authorization is what rejects it (401 or 403) on endpoints that require a user.
app.UseAuthentication();
app.UseAuthorization();
// After authentication, so write buckets are per user.
app.UseRateLimiter();
// After the rate limiter and before the endpoints. By default it skips requests with an Authorization header, so only
// anonymous reads are cached and one user's response is never served to another.
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
