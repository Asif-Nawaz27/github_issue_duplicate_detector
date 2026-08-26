using System.Text.Json;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.RepositoryId).IsRequired();
        builder.Property(i => i.GitHubIssueId).IsRequired();
        builder.Property(i => i.GitHubIssueNumber).IsRequired();
        builder.Property(i => i.Title).IsRequired().HasMaxLength(500);
        builder.Property(i => i.Body);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(2000);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.ClosedAt);

        builder.Property(i => i.State)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Labels)
            .HasField("_labels")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                labels => JsonSerializer.Serialize(labels, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        builder.Metadata
            .FindProperty(nameof(Issue.Labels))!
            .SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                a => a.Aggregate(0, (hash, label) => HashCode.Combine(hash, label)),
                a => a.ToList()));

        // Issue and Repository are separate aggregates; the relationship is enforced
        // only via this foreign key, with no navigation property on either side.
        builder.HasOne<Repository>()
            .WithMany()
            .HasForeignKey(i => i.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.GitHubIssueId).IsUnique();
        builder.HasIndex(i => new { i.RepositoryId, i.GitHubIssueNumber }).IsUnique();
        builder.HasIndex(i => i.State);
    }
}
