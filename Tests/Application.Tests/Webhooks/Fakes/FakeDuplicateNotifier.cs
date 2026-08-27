using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.Tests.Webhooks.Fakes;

internal sealed class FakeDuplicateNotifier : IDuplicateNotifier
{
    public List<(GitHubRepositoryInfo Repository, GitHubIssueInfo Issue, DuplicateDetectionResult Result)> Notifications { get; } = [];

    public Task NotifyAsync(
        GitHubRepositoryInfo repository, GitHubIssueInfo issue, DuplicateDetectionResult result, CancellationToken cancellationToken = default)
    {
        Notifications.Add((repository, issue, result));
        return Task.CompletedTask;
    }
}
