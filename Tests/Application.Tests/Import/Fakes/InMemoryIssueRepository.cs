using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryIssueRepository : IIssueRepository
{
    public List<Issue> Issues { get; } = [];

    public Task<Issue?> GetByRepositoryIdAndGitHubIssueIdAsync(Guid repositoryId, long gitHubIssueId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Issues.SingleOrDefault(i => i.RepositoryId == repositoryId && i.GitHubIssueId == gitHubIssueId));

    public void Add(Issue issue) => Issues.Add(issue);
}
