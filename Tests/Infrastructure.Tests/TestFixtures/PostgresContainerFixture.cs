using IssueSense.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IssueSense.Infrastructure.Tests.TestFixtures;

/// <summary>
/// Starts one real Postgres+pgvector container per test class, migrated once, so integration
/// tests exercise the actual EF Core mappings/constraints instead of an in-memory provider.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("issuesense_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_container.GetConnectionString());
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public IssueSenseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IssueSenseDbContext>()
            .UseNpgsql(_dataSource, npgsql => npgsql.UseVector())
            .Options;

        return new IssueSenseDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}
