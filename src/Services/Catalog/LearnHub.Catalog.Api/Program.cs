using LearnHub.Catalog.Api.Courses;
using LearnHub.Catalog.Api.Errors;
using LearnHub.Catalog.Application;
using LearnHub.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

builder.Services
    .AddCatalogApplication()
    .AddCatalogInfrastructure();

var app = builder.Build();

// First, so it catches exceptions thrown anywhere later in the pipeline.
app.UseExceptionHandler();
// Gives empty error responses (such as an unmatched route's 404) a Problem Details body.
app.UseStatusCodePages();

app.MapDefaultEndpoints();
app.MapCourseEndpoints();

app.Run();
