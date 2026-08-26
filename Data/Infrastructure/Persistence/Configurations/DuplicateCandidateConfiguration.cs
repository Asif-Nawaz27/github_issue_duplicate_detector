using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class DuplicateCandidateConfiguration : IEntityTypeConfiguration<DuplicateCandidate>
{
    public void Configure(EntityTypeBuilder<DuplicateCandidate> builder)
    {
        builder.ToTable("duplicate_candidates");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.NewIssueId).IsRequired();
        builder.Property(d => d.ExistingIssueId).IsRequired();
        builder.Property(d => d.DetectedAt).IsRequired();

        builder.Property(d => d.Score)
            .HasConversion(score => score.Value, value => SimilarityScore.Create(value))
            .IsRequired();

        // Restrict rather than cascade on both sides: deleting an issue that still has
        // recorded duplicate detections should be an explicit decision, not a side effect.
        builder.HasOne<Issue>()
            .WithMany()
            .HasForeignKey(d => d.NewIssueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Issue>()
            .WithMany()
            .HasForeignKey(d => d.ExistingIssueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.NewIssueId);
        builder.HasIndex(d => new { d.NewIssueId, d.ExistingIssueId }).IsUnique();
    }
}
