using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class IssueEmbeddingConfiguration : IEntityTypeConfiguration<IssueEmbedding>
{
    // OpenAI text-embedding-3-small and text-embedding-ada-002 both produce 1536-dimensional
    // vectors. Switching to a model with a different dimension requires a new migration.
    public const int EmbeddingDimensions = 1536;

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
