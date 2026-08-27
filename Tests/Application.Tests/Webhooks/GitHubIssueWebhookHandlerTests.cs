using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Tests.Webhooks.Fakes;
using IssueSense.Application.Webhooks;
using IssueSense.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueSense.Application.Tests.Webhooks;

public class GitHubIssueWebhookHandlerTests
{
    private readonly FakeDuplicateDetectionService _duplicateDetectionService = new();
    private readonly FakeDuplicateNotifier _duplicateNotifier = new();

    private static readonly GitHubRepositoryInfo Repository = new(1, "octocat", "hello-world", "https://github.com/octocat/hello-world");

    private GitHubIssueWebhookHandler CreateSut() =>
        new(_duplicateDetectionService, _duplicateNotifier, NullLogger<GitHubIssueWebhookHandler>.Instance);

    private static GitHubIssueInfo NewIssue(int number = 42) =>
        new(1000 + number, number, "App crashes on startup", "Steps to reproduce...", IssueState.Open,
            "https://github.com/octocat/hello-world/issues/" + number, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, []);

    [Fact]
    public async Task HandleAsync_WithOpenedAction_RunsDuplicateDetectionAndNotifies()
    {
        var issue = NewIssue();
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, issue);
        var sut = CreateSut();

        var result = await sut.HandleAsync(webhookEvent);

        Assert.True(result.Processed);
        Assert.Single(_duplicateDetectionService.Calls);
        Assert.Equal(("octocat", "hello-world"), _duplicateDetectionService.Calls[0]);
        Assert.Single(_duplicateNotifier.Notifications);
    }

    [Theory]
    [InlineData("closed")]
    [InlineData("edited")]
    [InlineData("reopened")]
    [InlineData("labeled")]
    public async Task HandleAsync_WithNonOpenedAction_SkipsProcessingAndDoesNotNotify(string action)
    {
        var webhookEvent = new GitHubIssueWebhookEvent(action, Repository, NewIssue());
        var sut = CreateSut();

        var result = await sut.HandleAsync(webhookEvent);

        Assert.False(result.Processed);
        Assert.Empty(_duplicateDetectionService.Calls);
        Assert.Empty(_duplicateNotifier.Notifications);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryNotImported_SkipsGracefullyWithoutThrowing()
    {
        _duplicateDetectionService.ThrowRepositoryNotFound = true;
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, NewIssue());
        var sut = CreateSut();

        var result = await sut.HandleAsync(webhookEvent);

        Assert.False(result.Processed);
        Assert.Contains("not been imported", result.Reason);
        Assert.Empty(_duplicateNotifier.Notifications);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCandidateCountAndHighestConfidence()
    {
        var candidate = new DuplicateCandidateMatch(
            7, "Existing issue", "https://github.com/octocat/hello-world/issues/7", 0.95,
            "octocat/hello-world", IssueState.Open, DuplicateConfidence.HighConfidence);
        _duplicateDetectionService.NextResult = new DuplicateDetectionResult([candidate], "test-model", 0.5);
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, NewIssue());
        var sut = CreateSut();

        var result = await sut.HandleAsync(webhookEvent);

        Assert.Equal(1, result.DuplicatesFound);
        Assert.Equal(DuplicateConfidence.HighConfidence, result.HighestConfidence);
    }

    [Fact]
    public async Task HandleAsync_WithNoDuplicatesFound_ReturnsNullHighestConfidence()
    {
        _duplicateDetectionService.NextResult = new DuplicateDetectionResult([], "test-model", 0.5);
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, NewIssue());
        var sut = CreateSut();

        var result = await sut.HandleAsync(webhookEvent);

        Assert.Equal(0, result.DuplicatesFound);
        Assert.Null(result.HighestConfidence);
    }

    [Fact]
    public async Task HandleAsync_PassesTheIssueThroughToDuplicateDetection()
    {
        var issue = NewIssue(number: 99);
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, issue);
        var sut = CreateSut();

        await sut.HandleAsync(webhookEvent);

        var notification = Assert.Single(_duplicateNotifier.Notifications);
        Assert.Equal(issue.Number, notification.Issue.Number);
        Assert.Equal(Repository.Owner, notification.Repository.Owner);
    }
}
