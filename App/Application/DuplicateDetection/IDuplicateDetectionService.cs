using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.DuplicateDetection;

/// <summary>
/// Finds existing issues that a new GitHub issue may duplicate, using vector similarity search
/// over previously generated embeddings. Read-only: this only produces recommendations and
/// never modifies or closes anything.
/// </summary>
public interface IDuplicateDetectionService
{
    Task<IReadOnlyList<DuplicateCandidateMatch>> FindDuplicatesAsync(
        string owner,
        string name,
        GitHubIssueInfo newIssue,
        CancellationToken cancellationToken = default);
}
