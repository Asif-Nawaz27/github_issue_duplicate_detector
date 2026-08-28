using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

/// <summary>Supports injecting failures on the comment-related calls, for testing notifier resilience.</summary>
internal sealed class FakeGitHubService : IGitHubService
{
    public List<GitHubComment> Comments { get; } = [];

    public List<(string Owner, string Name, int IssueNumber, string Body)> PostedComments { get; } = [];

    public Exception? GetCommentsFailure { get; set; }

    public Exception? PostCommentFailure { get; set; }

    public Task<GitHubRepositoryInfo> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public IAsyncEnumerable<GitHubIssueInfo> GetIssuesAsync(
        string owner, string name, GitHubIssueStateFilter state = GitHubIssueStateFilter.All, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task<GitHubIssueInfo> GetIssueAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task<IReadOnlyList<GitHubLabel>> GetIssueLabelsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by these tests.");

    public Task<IReadOnlyList<GitHubComment>> GetIssueCommentsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default)
    {
        if (GetCommentsFailure is not null)
            throw GetCommentsFailure;

        return Task.FromResult<IReadOnlyList<GitHubComment>>(Comments);
    }

    public Task<GitHubComment> PostIssueCommentAsync(string owner, string name, int issueNumber, string body, CancellationToken cancellationToken = default)
    {
        if (PostCommentFailure is not null)
            throw PostCommentFailure;

        PostedComments.Add((owner, name, issueNumber, body));
        var comment = new GitHubComment(PostedComments.Count, body, "issuesense-bot");
        Comments.Add(comment);

        return Task.FromResult(comment);
    }
}
