using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;

namespace IssueSense.Infrastructure.Tests.Import;

internal sealed class FakeGitHubService : IGitHubService
{
    public GitHubRepositoryInfo Repository { get; set; } = new(1, "octocat", "hello-world", "https://github.com/octocat/hello-world");

    public List<GitHubIssueInfo> Issues { get; set; } = [];

    public Task<GitHubRepositoryInfo> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Repository);

    public async IAsyncEnumerable<GitHubIssueInfo> GetIssuesAsync(
        string owner,
        string name,
        GitHubIssueStateFilter state = GitHubIssueStateFilter.All,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var issue in Issues)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return issue;
        }

        await Task.CompletedTask;
    }

    public Task<GitHubIssueInfo> GetIssueAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(Issues.Single(i => i.Number == issueNumber));

    public Task<IReadOnlyList<GitHubLabel>> GetIssueLabelsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GitHubLabel>>(
            Issues.Single(i => i.Number == issueNumber).Labels.Select(l => new GitHubLabel(l, "000000")).ToList());
}
