using IssueSense.Application.DuplicateDetection;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub.Webhooks;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

public class DuplicateCommentFormatterTests
{
    private static readonly DuplicateCandidateMatch Candidate = new(
        IssueNumber: 42,
        Title: "Existing crash issue",
        Url: "https://github.com/octocat/hello-world/issues/42",
        SimilarityScore: 0.934,
        Repository: "octocat/hello-world",
        State: IssueState.Open,
        Confidence: DuplicateConfidence.HighConfidence);

    [Fact]
    public void Format_AlwaysIncludesTheIdempotencyMarker()
    {
        var result = DuplicateCommentFormatter.Format(DuplicateCommentOptions.DefaultTemplate, Candidate);

        Assert.StartsWith(DuplicateCommentFormatter.IdempotencyMarker, result, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_IncludesMarkerEvenWithACustomTemplateThatDoesNotMentionIt()
    {
        var result = DuplicateCommentFormatter.Format("Just a custom message, no placeholders at all.", Candidate);

        Assert.Contains(DuplicateCommentFormatter.IdempotencyMarker, result, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SubstitutesAllSupportedPlaceholders()
    {
        const string template =
            "url={ExistingIssueUrl} number={ExistingIssueNumber} title={ExistingIssueTitle} " +
            "confidence={Confidence} score={SimilarityScore} percent={SimilarityPercent}";

        var result = DuplicateCommentFormatter.Format(template, Candidate);

        Assert.Contains("url=https://github.com/octocat/hello-world/issues/42", result, StringComparison.Ordinal);
        Assert.Contains("number=42", result, StringComparison.Ordinal);
        Assert.Contains("title=Existing crash issue", result, StringComparison.Ordinal);
        Assert.Contains("confidence=HighConfidence", result, StringComparison.Ordinal);
        Assert.Contains("score=0.93", result, StringComparison.Ordinal);
        Assert.Contains("percent=93", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WithDefaultTemplate_ProducesTheRequiredWarningLinkConfidenceAndConfirmationRequest()
    {
        var result = DuplicateCommentFormatter.Format(DuplicateCommentOptions.DefaultTemplate, Candidate);

        Assert.Contains("duplicate", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Candidate.Url, result, StringComparison.Ordinal);
        Assert.Contains("HighConfidence", result, StringComparison.Ordinal);
        Assert.Contains("93", result, StringComparison.Ordinal);
        Assert.Contains("different", result, StringComparison.OrdinalIgnoreCase);
    }
}
