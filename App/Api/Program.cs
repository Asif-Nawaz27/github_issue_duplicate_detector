using IssueSense.Api;
using IssueSense.Application;
using IssueSense.Infrastructure;
using IssueSense.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

const string WebDashboardCorsPolicy = "WebDashboard";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Origins come from config (Cors:AllowedOrigins) rather than being hardcoded, so
// deployments can allow their own frontend's origin without a code change. No configured
// origins means no cross-origin caller is allowed — fail closed, not open.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebDashboardCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Applying migrations on startup is a local-dev convenience so `dotnet run` against a
// fresh docker-compose Postgres just works; a real deployment would run migrations
// as an explicit release step instead.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IssueSenseDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors(WebDashboardCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes the implicit Program class accessible to WebApplicationFactory<Program> in test assemblies.
public partial class Program;
