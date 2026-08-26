using IssueSense.Application.GitHub.Models;

namespace IssueSense.Application.GitHub;

/// <summary>
/// Port for reading repository/issue data from GitHub. Implemented in Infrastructure against
/// the GitHub REST API; nothing here should leak HTTP, JSON, or GitHub wire-format concerns.
/// </summary>
public interface IGitHubService
{
    Task<GitHubRepositoryInfo> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default);

    /// <summary>Streams every issue in the repository, paging through GitHub's results as needed.</summary>
    IAsyncEnumerable<GitHubIssueInfo> GetIssuesAsync(
        string owner,
        string name,
        GitHubIssueStateFilter state = GitHubIssueStateFilter.All,
        CancellationToken cancellationToken = default);

    Task<GitHubIssueInfo> GetIssueAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitHubLabel>> GetIssueLabelsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default);
}
