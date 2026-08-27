using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.DuplicateDetection;

/// <summary>
/// Finds existing issues that a new GitHub issue may duplicate, using vector similarity search
/// over previously generated embeddings. Read-only: this only produces recommendations and
/// never modifies or closes anything.
/// </summary>
public interface IDuplicateDetectionService
{
    Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner,
        string name,
        GitHubIssueInfo newIssue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same check for an issue that doesn't have a GitHub identity yet — e.g. a title/body a
    /// caller is drafting before actually opening it. There's nothing to exclude by identity,
    /// since it can't already be stored.
    /// </summary>
    Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner,
        string name,
        string title,
        string? body,
        CancellationToken cancellationToken = default);
}
