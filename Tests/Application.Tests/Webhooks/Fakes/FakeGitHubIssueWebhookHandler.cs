using IssueSense.Application.Webhooks;

namespace IssueSense.Application.Tests.Webhooks.Fakes;

internal sealed class FakeGitHubIssueWebhookHandler : IGitHubIssueWebhookHandler
{
    public List<GitHubIssueWebhookEvent> Calls { get; } = [];

    public WebhookProcessingResult NextResult { get; set; } = new(Processed: true, Reason: "Processed.");

    public Task<WebhookProcessingResult> HandleAsync(GitHubIssueWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        Calls.Add(webhookEvent);
        return Task.FromResult(NextResult);
    }
}
