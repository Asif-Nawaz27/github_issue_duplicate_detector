using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.ToTable("repositories");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.GitHubRepositoryId).IsRequired();
        builder.Property(r => r.Owner).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        // Computed from Owner/Name; not a persisted column.
        builder.Ignore(r => r.FullName);

        builder.HasIndex(r => r.GitHubRepositoryId).IsUnique();
        builder.HasIndex(r => new { r.Owner, r.Name }).IsUnique();
    }
}
