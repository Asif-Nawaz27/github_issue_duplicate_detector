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
using Microsoft.Extensions.Options;
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

            // Read the "AppSettings" section from configuration exactly once; everything below
            // that needs a value from it (CORS, DuplicateDetectionOptions) uses this object, not
            // another configuration.GetSection(...) call of its own.
            var appSettings = configuration.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();
            services.AddSingleton(appSettings);
            services.AddSingleton(Options.Create(appSettings));

            services.AddApplication(appSettings);
            services.AddInfrastructure(configuration);
            services.AddCorsPolicy(appSettings);

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

        private static IServiceCollection AddApplication(this IServiceCollection services, AppSettings appSettings)
        {
            services.AddScoped<IIssueImportService, IssueImportService>();
            services.AddScoped<IEmbeddingGenerationService, EmbeddingGenerationService>();
            services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
            services.AddScoped<IGitHubIssueWebhookHandler, GitHubIssueWebhookHandler>();
            services.AddScoped<IOwnerService, OwnerService>();
            services.AddScoped<IRepositoryLookupService, RepositoryLookupService>();

            // DuplicateDetectionService only needs IOptions<DuplicateDetectionOptions>, not the
            // whole AppSettings — wraps the exact same DuplicateDetectionOptions instance already
            // bound inside appSettings, so Application never needs to know AppSettings (an
            // Api-layer type) exists, and nothing here re-reads configuration.
            services.AddSingleton(Options.Create(appSettings.DuplicateDetection));

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

        private static IServiceCollection AddCorsPolicy(this IServiceCollection services, AppSettings appSettings)
        {
            // Origins come from appSettings.CorsAllowedOrigins (bound once, in AddMiddleware)
            // rather than being hardcoded, so deployments can allow their own frontend's origin
            // without a code change. No configured origins means no cross-origin caller is
            // allowed — fail closed, not open.
            services.AddCors(options =>
            {
                options.AddPolicy(WebDashboardCorsPolicy, policy =>
                {
                    if (appSettings.CorsAllowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(appSettings.CorsAllowedOrigins).AllowAnyHeader().AllowAnyMethod();
                    }
                });
            });

            return services;
        }
    }
}
