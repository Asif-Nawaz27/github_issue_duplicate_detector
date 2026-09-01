using IssueSense.Api;
using IssueSense.Api.Infrastructure;
using IssueSense.Application;
using IssueSense.Infrastructure;
using IssueSense.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

const string WebDashboardCorsPolicy = "WebDashboard";

var builder = WebApplication.CreateBuilder(args);

// utilize middleware method

builder.Services.AddMiddleware();


builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// add cors policy
builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

//dbcontext migration with db.
app.AddDBContext();

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
