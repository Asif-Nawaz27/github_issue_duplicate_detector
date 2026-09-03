using IssueSense.Application.Webhooks;

namespace IssueSense.Application.Tests.Webhooks.Fakes;

internal sealed class FakeGitHubWebhookParser : IGitHubWebhookParser
{
    public GitHubIssueWebhookEvent? NextResult { get; set; }

    public GitHubIssueWebhookEvent? ParseIssueEvent(byte[] payload) => NextResult;
}
