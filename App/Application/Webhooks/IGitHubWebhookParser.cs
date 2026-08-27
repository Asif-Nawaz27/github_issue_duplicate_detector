namespace IssueSense.Application.Webhooks;

/// <summary>Parses a GitHub "issues" webhook payload. GitHub wire-format details stay in Infrastructure.</summary>
public interface IGitHubWebhookParser
{
    /// <summary>
    /// Returns the parsed event, or null if the payload isn't a usable issue event — malformed
    /// JSON, missing issue/repository data, or a pull request (issues and PRs share a payload
    /// shape, so this is filtered here rather than left to callers).
    /// </summary>
    GitHubIssueWebhookEvent? ParseIssueEvent(byte[] payload);
}
