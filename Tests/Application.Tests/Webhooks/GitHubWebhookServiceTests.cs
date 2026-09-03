using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Tests.Webhooks.Fakes;
using IssueSense.Application.Webhooks;
using IssueSense.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueSense.Application.Tests.Webhooks;

public class GitHubWebhookServiceTests
{
    private readonly FakeGitHubWebhookSignatureVerifier _signatureVerifier = new();
    private readonly FakeGitHubWebhookParser _webhookParser = new();
    private readonly FakeGitHubIssueWebhookHandler _webhookHandler = new();

    private static readonly byte[] ValidJsonPayload = "{}"u8.ToArray();
    private static readonly byte[] MalformedJsonPayload = "not json"u8.ToArray();

    private static readonly GitHubRepositoryInfo Repository = new(1, "octocat", "hello-world", "https://github.com/octocat/hello-world");

    private GitHubWebhookService CreateSut() =>
        new(_signatureVerifier, _webhookParser, _webhookHandler, NullLogger<GitHubWebhookService>.Instance);

    private static GitHubIssueInfo NewIssue(int number = 42) =>
        new(1000 + number, number, "App crashes on startup", "Steps to reproduce...", IssueState.Open,
            "https://github.com/octocat/hello-world/issues/" + number, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, []);

    [Fact]
    public async Task ProcessAsync_WithInvalidSignature_ReturnsInvalidSignatureAndSkipsEverythingElse()
    {
        _signatureVerifier.IsValidResult = false;
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=bogus", "issues");

        Assert.Equal(GitHubWebhookStatus.InvalidSignature, result.Status);
        Assert.Empty(_webhookHandler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_WithPingEvent_ReturnsPingWithoutParsingOrHandling()
    {
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=valid", "ping");

        Assert.Equal(GitHubWebhookStatus.Ping, result.Status);
        Assert.Equal("pong", result.Message);
        Assert.Empty(_webhookHandler.Calls);
    }

    [Theory]
    [InlineData("pull_request")]
    [InlineData(null)]
    public async Task ProcessAsync_WithNonIssuesEvent_ReturnsIgnoredWithoutParsingOrHandling(string? eventType)
    {
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=valid", eventType);

        Assert.Equal(GitHubWebhookStatus.Ignored, result.Status);
        Assert.Empty(_webhookHandler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_WithMalformedJson_ReturnsMalformedPayload()
    {
        var sut = CreateSut();

        var result = await sut.ProcessAsync(MalformedJsonPayload, "sha256=valid", "issues");

        Assert.Equal(GitHubWebhookStatus.MalformedPayload, result.Status);
        Assert.Empty(_webhookHandler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_WhenParserReturnsNull_ReturnsIgnoredWithoutHandling()
    {
        _webhookParser.NextResult = null;
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=valid", "issues");

        Assert.Equal(GitHubWebhookStatus.Ignored, result.Status);
        Assert.Empty(_webhookHandler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_WithValidIssuesEvent_HandsOffToWebhookHandlerAndReturnsProcessed()
    {
        var webhookEvent = new GitHubIssueWebhookEvent("opened", Repository, NewIssue());
        _webhookParser.NextResult = webhookEvent;
        _webhookHandler.NextResult = new WebhookProcessingResult(Processed: true, Reason: "Processed.", DuplicatesFound: 2);
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=valid", "issues");

        Assert.Equal(GitHubWebhookStatus.Processed, result.Status);
        Assert.True(result.Processed);
        Assert.Equal(2, result.DuplicatesFound);
        Assert.Equal("Processed.", result.Message);
        var handled = Assert.Single(_webhookHandler.Calls);
        Assert.Same(webhookEvent, handled);
    }

    [Fact]
    public async Task ProcessAsync_WithValidIssuesEventButHandlerSkips_ReturnsProcessedFalse()
    {
        _webhookParser.NextResult = new GitHubIssueWebhookEvent("closed", Repository, NewIssue());
        _webhookHandler.NextResult = new WebhookProcessingResult(Processed: false, Reason: "Ignored: action 'closed' is not 'opened'.");
        var sut = CreateSut();

        var result = await sut.ProcessAsync(ValidJsonPayload, "sha256=valid", "issues");

        Assert.Equal(GitHubWebhookStatus.Processed, result.Status);
        Assert.False(result.Processed);
        Assert.Equal(0, result.DuplicatesFound);
    }
}
