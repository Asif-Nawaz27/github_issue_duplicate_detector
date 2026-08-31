using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence;

public sealed class IssueSenseDbContext(DbContextOptions<IssueSenseDbContext> options) : DbContext(options)
{
    public DbSet<Repository> Repositories => Set<Repository>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<IssueEmbedding> IssueEmbeddings => Set<IssueEmbedding>();

    public DbSet<DuplicateCandidate> DuplicateCandidates => Set<DuplicateCandidate>();

    public DbSet<Owner> Owners => Set<Owner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IssueSenseDbContext).Assembly);
    }
}
