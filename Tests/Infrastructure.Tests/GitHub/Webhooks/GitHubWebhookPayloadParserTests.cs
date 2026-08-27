using System.Text;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub.Webhooks;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

public class GitHubWebhookPayloadParserTests
{
    private readonly GitHubWebhookPayloadParser _sut = new();

    private static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseIssueEvent_WithValidIssueOpenedPayload_ReturnsMappedEvent()
    {
        var payload = Bytes("""
            {
                "action": "opened",
                "issue": {
                    "id": 123456,
                    "number": 42,
                    "title": "App crashes on startup",
                    "body": "Steps to reproduce...",
                    "state": "open",
                    "html_url": "https://github.com/octocat/hello-world/issues/42",
                    "created_at": "2026-01-01T00:00:00Z",
                    "updated_at": "2026-01-01T00:00:00Z",
                    "closed_at": null,
                    "labels": [ { "name": "bug", "color": "d73a4a" } ]
                },
                "repository": {
                    "id": 1,
                    "name": "hello-world",
                    "html_url": "https://github.com/octocat/hello-world",
                    "owner": { "login": "octocat" }
                }
            }
            """);

        var result = _sut.ParseIssueEvent(payload);

        Assert.NotNull(result);
        Assert.Equal("opened", result.Action);
        Assert.Equal("octocat", result.Repository.Owner);
        Assert.Equal("hello-world", result.Repository.Name);
        Assert.Equal(42, result.Issue.Number);
        Assert.Equal("App crashes on startup", result.Issue.Title);
        Assert.Equal(IssueState.Open, result.Issue.State);
        Assert.Equal(["bug"], result.Issue.Labels);
    }

    [Fact]
    public void ParseIssueEvent_WithPullRequestPayload_ReturnsNull()
    {
        var payload = Bytes("""
            {
                "action": "opened",
                "issue": {
                    "id": 1, "number": 1, "title": "A pull request", "state": "open",
                    "html_url": "https://github.com/o/r/issues/1",
                    "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z",
                    "labels": [],
                    "pull_request": { "url": "https://api.github.com/repos/o/r/pulls/1" }
                },
                "repository": { "id": 1, "name": "r", "html_url": "https://github.com/o/r", "owner": { "login": "o" } }
            }
            """);

        Assert.Null(_sut.ParseIssueEvent(payload));
    }

    [Fact]
    public void ParseIssueEvent_WithMissingIssue_ReturnsNull()
    {
        var payload = Bytes("""
            {
                "action": "opened",
                "repository": { "id": 1, "name": "r", "html_url": "https://github.com/o/r", "owner": { "login": "o" } }
            }
            """);

        Assert.Null(_sut.ParseIssueEvent(payload));
    }

    [Fact]
    public void ParseIssueEvent_WithMissingRepository_ReturnsNull()
    {
        var payload = Bytes("""
            {
                "action": "opened",
                "issue": {
                    "id": 1, "number": 1, "title": "t", "state": "open",
                    "html_url": "https://github.com/o/r/issues/1",
                    "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z", "labels": []
                }
            }
            """);

        Assert.Null(_sut.ParseIssueEvent(payload));
    }

    [Fact]
    public void ParseIssueEvent_WithMalformedJson_ReturnsNull()
    {
        var payload = Bytes("{not valid json");

        Assert.Null(_sut.ParseIssueEvent(payload));
    }

    [Fact]
    public void ParseIssueEvent_WithClosedAction_StillParsesEvent()
    {
        var payload = Bytes("""
            {
                "action": "closed",
                "issue": {
                    "id": 1, "number": 1, "title": "t", "state": "closed",
                    "html_url": "https://github.com/o/r/issues/1",
                    "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z", "labels": []
                },
                "repository": { "id": 1, "name": "r", "html_url": "https://github.com/o/r", "owner": { "login": "o" } }
            }
            """);

        var result = _sut.ParseIssueEvent(payload);

        Assert.NotNull(result);
        Assert.Equal("closed", result.Action);
    }
}
