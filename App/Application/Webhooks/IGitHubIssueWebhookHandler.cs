namespace IssueSense.Application.Webhooks;

/// <summary>
/// Orchestrates what happens once a webhook delivery has been verified and parsed: run duplicate
/// detection for newly-opened issues, notify on the result, and skip anything else gracefully.
/// </summary>
public interface IGitHubIssueWebhookHandler
{
    Task<WebhookProcessingResult> HandleAsync(GitHubIssueWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
