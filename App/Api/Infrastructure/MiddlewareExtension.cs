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

namespace IssueSense.Api.Infrastructure
{
    public static class MiddlewareExtension
    {
        const string WebDashboardCorsPolicy = "IssueSenseDashboard";

        // Add services to the container.
        public static void AddMiddleware(this IServiceCollection services) {

            //add controller service
            services.AddControllers();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            services.AddOpenApi();

        }

        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
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

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPersistence(configuration);
            services.AddGitHubClient(configuration);
            services.AddEmbeddings(configuration);

            return services;
        }


        public static void AddCorsPolicy(this IServiceCollection services, ConfigurationManager configuration)
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
        }


        public static async void AddDBContext(this WebApplication app)
        {
            // Applying migrations on startup is a local-dev convenience so `dotnet run` against a
            // fresh docker-compose Postgres just works; a real deployment would run migrations
            // as an explicit release step instead.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IssueSenseDbContext>();
                await dbContext.Database.MigrateAsync();
            }
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
        

    }
}
