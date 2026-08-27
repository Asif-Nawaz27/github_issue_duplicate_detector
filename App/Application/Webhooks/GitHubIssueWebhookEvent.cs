using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.Webhooks;

/// <summary>
/// An already-verified, already-parsed GitHub "issues" webhook delivery. By construction this
/// never represents a pull request — the parser filters those out before this type can exist.
/// </summary>
public sealed record GitHubIssueWebhookEvent(string Action, GitHubRepositoryInfo Repository, GitHubIssueInfo Issue);
