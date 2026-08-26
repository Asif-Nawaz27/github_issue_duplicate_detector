using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Tests.Entities;

public class DuplicateCandidateTests
{
    private static readonly DateTimeOffset DetectedAt = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var newIssueId = Guid.NewGuid();
        var existingIssueId = Guid.NewGuid();
        var score = SimilarityScore.Create(0.92);

        var candidate = DuplicateCandidate.Create(newIssueId, existingIssueId, score, DetectedAt);

        Assert.NotEqual(Guid.Empty, candidate.Id);
        Assert.Equal(newIssueId, candidate.NewIssueId);
        Assert.Equal(existingIssueId, candidate.ExistingIssueId);
        Assert.Equal(score, candidate.Score);
        Assert.Equal(DetectedAt, candidate.DetectedAt);
    }

    [Fact]
    public void Create_WithEmptyNewIssueId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => DuplicateCandidate.Create(Guid.Empty, Guid.NewGuid(), SimilarityScore.Create(0.5), DetectedAt));
    }

    [Fact]
    public void Create_WithEmptyExistingIssueId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => DuplicateCandidate.Create(Guid.NewGuid(), Guid.Empty, SimilarityScore.Create(0.5), DetectedAt));
    }

    [Fact]
    public void Create_WithSameNewAndExistingIssueId_Throws()
    {
        var issueId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () => DuplicateCandidate.Create(issueId, issueId, SimilarityScore.Create(0.5), DetectedAt));
    }
}
