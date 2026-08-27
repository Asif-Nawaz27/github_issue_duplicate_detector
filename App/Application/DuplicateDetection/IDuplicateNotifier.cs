using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.DuplicateDetection;

/// <summary>
/// Reacts to a completed duplicate check. The initial implementation only logs; swapping in one
/// that posts a GitHub comment for high-confidence matches later is a matter of registering a
/// different implementation here — nothing that calls this port needs to change.
/// </summary>
public interface IDuplicateNotifier
{
    Task NotifyAsync(
        GitHubRepositoryInfo repository,
        GitHubIssueInfo issue,
        DuplicateDetectionResult result,
        CancellationToken cancellationToken = default);
}
