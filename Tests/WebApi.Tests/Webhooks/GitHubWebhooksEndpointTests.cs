using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace IssueSense.WebApi.Tests.Webhooks;

public sealed class GitHubWebhooksEndpointTests : IClassFixture<WebhookTestWebApplicationFactory>
{
    private readonly WebhookTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GitHubWebhooksEndpointTests(WebhookTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // The factory (and its fake) is shared across every test in this class via
        // IClassFixture; reset per-test state here since xUnit creates a fresh test class
        // instance (and re-runs this constructor) for each test method.
        _factory.DuplicateDetectionService.Calls.Clear();
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string IssueEventPayload(string action = "opened") => $$"""
        {
            "action": "{{action}}",
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
                "labels": []
            },
            "repository": {
                "id": 1,
                "name": "hello-world",
                "html_url": "https://github.com/octocat/hello-world",
                "owner": { "login": "octocat" }
            }
        }
        """;

    private static string PullRequestEventPayload() => """
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
        """;

    private static HttpRequestMessage CreateRequest(string payload, string? signature, string? eventType = "issues")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (signature is not null)
            request.Headers.Add("X-Hub-Signature-256", signature);

        if (eventType is not null)
            request.Headers.Add("X-GitHub-Event", eventType);

        return request;
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithValidSignatureAndIssueOpened_ReturnsOkAndProcessesEvent()
    {
        var payload = IssueEventPayload();
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(_factory.DuplicateDetectionService.Calls);
        Assert.Equal(("octocat", "hello-world"), _factory.DuplicateDetectionService.Calls[0]);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithInvalidSignature_ReturnsUnauthorizedAndDoesNotProcess()
    {
        var payload = IssueEventPayload();
        var request = CreateRequest(payload, "sha256=" + new string('0', 64));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithMissingSignature_ReturnsUnauthorized()
    {
        var payload = IssueEventPayload();
        var request = CreateRequest(payload, signature: null);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithTamperedPayload_ReturnsUnauthorized()
    {
        var payload = IssueEventPayload();
        var signature = ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret);
        var tamperedPayload = payload.Replace("App crashes", "Something else entirely");
        var request = CreateRequest(tamperedPayload, signature);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithPingEvent_ReturnsOkWithoutProcessing()
    {
        const string payload = "{}";
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret), eventType: "ping");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithNonIssueEventType_ReturnsOkWithoutProcessing()
    {
        var payload = IssueEventPayload();
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret), eventType: "push");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithNonOpenedAction_ReturnsOkWithoutProcessing()
    {
        var payload = IssueEventPayload(action: "closed");
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithPullRequestPayload_ReturnsOkWithoutProcessing()
    {
        var payload = PullRequestEventPayload();
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithMalformedJson_ReturnsBadRequest()
    {
        const string payload = "{not valid json";
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveGitHubWebhook_WithMissingEventHeader_ReturnsOkWithoutProcessing()
    {
        var payload = IssueEventPayload();
        var request = CreateRequest(payload, ComputeSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret), eventType: null);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.DuplicateDetectionService.Calls);
    }
}
