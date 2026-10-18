using LearnHub.Catalog.Api.Courses;
using LearnHub.Catalog.Application;
using LearnHub.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddCatalogApplication()
    .AddCatalogInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapCourseEndpoints();

app.Run();
