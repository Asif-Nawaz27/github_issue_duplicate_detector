using IssueSense.Domain.Common;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Entities;

public sealed class DuplicateCandidate : Entity
{
    public Guid NewIssueId { get; }

    public Guid ExistingIssueId { get; }

    public SimilarityScore Score { get; }

    public DateTimeOffset DetectedAt { get; }

    private DuplicateCandidate(Guid id, Guid newIssueId, Guid existingIssueId, SimilarityScore score, DateTimeOffset detectedAt)
        : base(id)
    {
        NewIssueId = newIssueId;
        ExistingIssueId = existingIssueId;
        Score = score;
        DetectedAt = detectedAt;
    }

    public static DuplicateCandidate Create(Guid newIssueId, Guid existingIssueId, SimilarityScore score, DateTimeOffset detectedAt)
    {
        if (newIssueId == Guid.Empty)
            throw new ArgumentException("New issue id is required.", nameof(newIssueId));
        if (existingIssueId == Guid.Empty)
            throw new ArgumentException("Existing issue id is required.", nameof(existingIssueId));
        if (newIssueId == existingIssueId)
            throw new ArgumentException("An issue cannot be a duplicate candidate of itself.", nameof(existingIssueId));

        return new DuplicateCandidate(Guid.NewGuid(), newIssueId, existingIssueId, score, detectedAt);
    }
}
