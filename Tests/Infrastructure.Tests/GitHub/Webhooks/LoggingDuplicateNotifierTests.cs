using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

public class LoggingDuplicateNotifierTests
{
    private readonly LoggingDuplicateNotifier _sut = new(NullLogger<LoggingDuplicateNotifier>.Instance);

    private static readonly GitHubRepositoryInfo Repository = new(1, "octocat", "hello-world", "https://github.com/octocat/hello-world");
    private static readonly GitHubIssueInfo Issue = new(
        99, 42, "New issue", "Body", IssueState.Open, "https://github.com/octocat/hello-world/issues/42",
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, []);

    [Fact]
    public async Task NotifyAsync_WithNoCandidates_DoesNotThrow()
    {
        var result = new DuplicateDetectionResult([], "model", 0.5);

        await _sut.NotifyAsync(Repository, Issue, result);
    }

    [Fact]
    public async Task NotifyAsync_WithHighConfidenceCandidate_DoesNotThrow()
    {
        var candidate = new DuplicateCandidateMatch(10, "Existing issue", "https://github.com/o/r/issues/10", 0.95, "octocat/hello-world", IssueState.Open, DuplicateConfidence.HighConfidence);
        var result = new DuplicateDetectionResult([candidate], "model", 0.5);

        await _sut.NotifyAsync(Repository, Issue, result);
    }

    [Fact]
    public async Task NotifyAsync_WithPossibleCandidate_DoesNotThrow()
    {
        var candidate = new DuplicateCandidateMatch(10, "Existing issue", "https://github.com/o/r/issues/10", 0.8, "octocat/hello-world", IssueState.Open, DuplicateConfidence.Possible);
        var result = new DuplicateDetectionResult([candidate], "model", 0.5);

        await _sut.NotifyAsync(Repository, Issue, result);
    }
}
