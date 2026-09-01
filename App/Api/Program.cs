using IssueSense.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMiddleware(builder.Configuration);

var app = builder.Build();

await app.UseMiddlewareAsync();

app.Run();

// Makes the implicit Program class accessible to WebApplicationFactory<Program> in test assemblies.
public partial class Program;
