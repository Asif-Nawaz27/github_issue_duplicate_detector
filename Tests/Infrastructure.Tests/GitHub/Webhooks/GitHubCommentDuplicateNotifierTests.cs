using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

public class GitHubCommentDuplicateNotifierTests
{
    private readonly FakeGitHubService _gitHubService = new();

    private static readonly GitHubRepositoryInfo Repository = new(1, "octocat", "hello-world", "https://github.com/octocat/hello-world");
    private static readonly GitHubIssueInfo Issue = new(
        99, 42, "New issue", "Body", IssueState.Open, "https://github.com/octocat/hello-world/issues/42",
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, []);

    private GitHubCommentDuplicateNotifier CreateSut(DuplicateCommentOptions? options = null) =>
        new(_gitHubService, Options.Create(options ?? new DuplicateCommentOptions()), NullLogger<GitHubCommentDuplicateNotifier>.Instance);

    private static DuplicateCandidateMatch Candidate(double similarity, DuplicateConfidence confidence, int issueNumber = 7) =>
        new(issueNumber, "Existing issue", $"https://github.com/octocat/hello-world/issues/{issueNumber}", similarity, "octocat/hello-world", IssueState.Open, confidence);

    [Fact]
    public async Task NotifyAsync_WithHighConfidenceDuplicate_PostsCommentContainingWarningLinkAndConfidence()
    {
        var candidate = Candidate(0.95, DuplicateConfidence.HighConfidence);
        var result = new DuplicateDetectionResult([candidate], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);

        var posted = Assert.Single(_gitHubService.PostedComments);
        Assert.Equal("octocat", posted.Owner);
        Assert.Equal("hello-world", posted.Name);
        Assert.Equal(42, posted.IssueNumber);
        Assert.Contains(candidate.Url, posted.Body, StringComparison.Ordinal);
        Assert.Contains("HighConfidence", posted.Body, StringComparison.Ordinal);
        Assert.Contains("duplicate", posted.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("different", posted.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NotifyAsync_WithHighConfidenceDuplicate_CommentIncludesIdempotencyMarker()
    {
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);

        var posted = Assert.Single(_gitHubService.PostedComments);
        Assert.Contains(DuplicateCommentFormatter.IdempotencyMarker, posted.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotifyAsync_WithLowConfidenceMatch_DoesNotPostAComment()
    {
        var result = new DuplicateDetectionResult([Candidate(0.6, DuplicateConfidence.Possible)], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);

        Assert.Empty(_gitHubService.PostedComments);
    }

    [Fact]
    public async Task NotifyAsync_WithNoCandidates_DoesNotPostAComment()
    {
        var result = new DuplicateDetectionResult([], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);

        Assert.Empty(_gitHubService.PostedComments);
    }

    [Fact]
    public async Task NotifyAsync_WhenWebhookIsRedelivered_DoesNotPostASecondComment()
    {
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);
        await sut.NotifyAsync(Repository, Issue, result); // Simulates GitHub redelivering the same webhook.

        Assert.Single(_gitHubService.PostedComments);
    }

    [Fact]
    public async Task NotifyAsync_WhenACommentWithTheMarkerAlreadyExists_DoesNotPostAnother()
    {
        _gitHubService.Comments.Add(new GitHubComment(1, $"{DuplicateCommentFormatter.IdempotencyMarker}\nAlready warned about this.", "issuesense-bot"));
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        await sut.NotifyAsync(Repository, Issue, result);

        Assert.Empty(_gitHubService.PostedComments);
    }

    [Fact]
    public async Task NotifyAsync_WhenPostingFailsWithAGenericGitHubApiError_DoesNotThrow()
    {
        _gitHubService.PostCommentFailure = new HttpRequestException("GitHub is having a bad day.");
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.NotifyAsync(Repository, Issue, result));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyAsync_WhenListingCommentsFailsWithAGenericGitHubApiError_DoesNotThrow()
    {
        _gitHubService.GetCommentsFailure = new HttpRequestException("GitHub is having a bad day.");
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.NotifyAsync(Repository, Issue, result));

        Assert.Null(exception);
        Assert.Empty(_gitHubService.PostedComments);
    }

    [Fact]
    public async Task NotifyAsync_WhenPostingFailsWithPermissionDenied_DoesNotThrow()
    {
        _gitHubService.PostCommentFailure = new GitHubForbiddenException("Token lacks issues:write scope.");
        var result = new DuplicateDetectionResult([Candidate(0.95, DuplicateConfidence.HighConfidence)], "model", 0.5);
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.NotifyAsync(Repository, Issue, result));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyAsync_UsesTheConfiguredCommentTemplate()
    {
        var options = new DuplicateCommentOptions { Template = "Custom warning: see {ExistingIssueUrl} ({SimilarityPercent}%)." };
        var candidate = Candidate(0.95, DuplicateConfidence.HighConfidence);
        var result = new DuplicateDetectionResult([candidate], "model", 0.5);
        var sut = CreateSut(options);

        await sut.NotifyAsync(Repository, Issue, result);

        var posted = Assert.Single(_gitHubService.PostedComments);
        Assert.Contains($"Custom warning: see {candidate.Url} (95%).", posted.Body, StringComparison.Ordinal);
    }
}
