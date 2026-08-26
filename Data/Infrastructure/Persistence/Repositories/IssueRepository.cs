using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class IssueRepository(IssueSenseDbContext dbContext) : IIssueRepository
{
    public Task<Issue?> GetByRepositoryIdAndGitHubIssueIdAsync(Guid repositoryId, long gitHubIssueId, CancellationToken cancellationToken = default) =>
        dbContext.Issues.SingleOrDefaultAsync(i => i.RepositoryId == repositoryId && i.GitHubIssueId == gitHubIssueId, cancellationToken);

    public void Add(Issue issue) => dbContext.Issues.Add(issue);
}
