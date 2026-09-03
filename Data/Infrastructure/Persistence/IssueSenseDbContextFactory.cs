using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace IssueSense.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` commands build the model without starting the full Api host.
/// Only used at design time (migrations); the running app resolves the context via DI instead.
/// </summary>
public sealed class IssueSenseDbContextFactory : IDesignTimeDbContextFactory<IssueSenseDbContext>
{
    public IssueSenseDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? string.Empty;

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<IssueSenseDbContext>();
        optionsBuilder.UseNpgsql(dataSource, npgsql => npgsql.UseVector());

        return new IssueSenseDbContext(optionsBuilder.Options);
    }
}
