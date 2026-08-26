using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class IssueEmbeddingConfiguration : IEntityTypeConfiguration<IssueEmbedding>
{
    // Matches the output size of the currently configured embedding model (all-MiniLM-L6-v2,
    // see LocalEmbeddingOptions.Dimensions). Switching to a model with a different output size
    // requires updating this constant and adding a new migration to alter the column.
    public const int EmbeddingDimensions = 384;

    public void Configure(EntityTypeBuilder<IssueEmbedding> builder)
    {
        builder.ToTable("issue_embeddings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.IssueId).IsRequired();
        builder.Property(e => e.ModelName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.Vector)
            .HasConversion(
                vector => new Vector(vector.Values.ToArray()),
                vector => EmbeddingVector.Create(vector.ToArray()))
            .HasColumnType($"vector({EmbeddingDimensions})")
            .IsRequired();

        // One embedding per issue for v1; re-embedding overwrites rather than versions.
        builder.HasIndex(e => e.IssueId).IsUnique();

        builder.HasIndex(e => e.Vector)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasOne<Issue>()
            .WithOne()
            .HasForeignKey<IssueEmbedding>(e => e.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
