using IssueSense.Application.Persistence;
using IssueSense.Infrastructure.Embeddings;
using IssueSense.Infrastructure.GitHub;
using IssueSense.Infrastructure.Persistence;
using IssueSense.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace IssueSense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IIssueEmbeddingRepository, IssueEmbeddingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
