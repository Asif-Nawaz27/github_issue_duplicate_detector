using IssueSense.Application;
using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Embeddings;
using IssueSense.Application.Import;
using IssueSense.Application.Owners;
using IssueSense.Application.Repositories;
using IssueSense.Application.Webhooks;
using IssueSense.Infrastructure;
using IssueSense.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

namespace IssueSense.Api.Infrastructure
{
    /// <summary>
    /// Everything Program.cs needs to register services and configure the HTTP pipeline lives
    /// here — one place to read appsettings/configuration and wire it up, instead of that being
    /// spread across Program.cs and every layer's own DI extension.
    /// </summary>
    public static class MiddlewareExtension
    {
        const string WebDashboardCorsPolicy = "IssueSenseDashboard";

        // Everything Program.cs calls to register services, in one place.
        public static IServiceCollection AddMiddleware(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            services.AddOpenApi();

            services.AddApplication(configuration);
            services.AddInfrastructure(configuration);
            services.AddCorsPolicy(configuration);

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        // Everything Program.cs calls to configure the HTTP pipeline, in one place.
        public static async Task<WebApplication> UseMiddlewareAsync(this WebApplication app)
        {
            app.UseExceptionHandler();

            // Applying migrations on startup is a local-dev convenience so `dotnet run` against a
            // fresh docker-compose Postgres just works; a real deployment would run migrations
            // as an explicit release step instead.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IssueSenseDbContext>();
                await dbContext.Database.MigrateAsync();

                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            // Same constant used to register (AddCorsPolicy) and apply the policy — registering
            // one name and applying another is a silent no-op, not an error, so keeping this in
            // one place instead of a second copy in Program.cs is what actually prevents that.
            app.UseCors(WebDashboardCorsPolicy);

            app.UseAuthorization();

            app.MapControllers();

            return app;
        }

        private static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IIssueImportService, IssueImportService>();
            services.AddScoped<IEmbeddingGenerationService, EmbeddingGenerationService>();
            services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
            services.AddScoped<IGitHubIssueWebhookHandler, GitHubIssueWebhookHandler>();
            services.AddScoped<IOwnerService, OwnerService>();
            services.AddScoped<IRepositoryLookupService, RepositoryLookupService>();

            services.AddOptions<DuplicateDetectionOptions>()
                .Bind(configuration.GetSection(DuplicateDetectionOptions.SectionName));

            return services;
        }

        private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPersistence(configuration);
            services.AddGitHubClient(configuration);
            services.AddEmbeddings(configuration);

            return services;
        }

        private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Postgres");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Postgres' is not configured. Set ConnectionStrings:Postgres in " +
                    "appsettings or the ConnectionStrings__Postgres environment variable.");
            }

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();

            services.AddSingleton(dataSource);
            services.AddDbContext<IssueSenseDbContext>(options => options.UseNpgsql(dataSource, npgsql => npgsql.UseVector()));

            services.RegisterInfrastructureServices();

            return services;
        }

        private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            // Origins come from config (Cors:AllowedOrigins) rather than being hardcoded, so
            // deployments can allow their own frontend's origin without a code change. No configured
            // origins means no cross-origin caller is allowed — fail closed, not open.
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            services.AddCors(options =>
            {
                options.AddPolicy(WebDashboardCorsPolicy, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                    }
                });
            });

            return services;
        }
    }
}
