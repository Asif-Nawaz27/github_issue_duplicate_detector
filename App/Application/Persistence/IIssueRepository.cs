using IssueSense.Domain.Entities;

namespace IssueSense.Application.Persistence;

public interface IIssueRepository
{
    Task<Issue?> GetByRepositoryIdAndGitHubIssueIdAsync(Guid repositoryId, long gitHubIssueId, CancellationToken cancellationToken = default);

    void Add(Issue issue);
}
