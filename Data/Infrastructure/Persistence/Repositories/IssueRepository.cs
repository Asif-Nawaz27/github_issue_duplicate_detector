using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class IssueRepository(IssueSenseDbContext dbContext) : IIssueRepository
{
    public Task<Issue?> GetByRepositoryIdAndGitHubIssueIdAsync(Guid repositoryId, long gitHubIssueId, CancellationToken cancellationToken = default) =>
        dbContext.Issues.SingleOrDefaultAsync(i => i.RepositoryId == repositoryId && i.GitHubIssueId == gitHubIssueId, cancellationToken);

    public Task<int> CountByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default) =>
        dbContext.Issues.CountAsync(i => i.RepositoryId == repositoryId, cancellationToken);

    public async Task<IReadOnlyList<Issue>> GetWithoutEmbeddingAsync(Guid repositoryId, CancellationToken cancellationToken = default) =>
        await dbContext.Issues
            .Where(i => i.RepositoryId == repositoryId && !dbContext.IssueEmbeddings.Any(e => e.IssueId == i.Id))
            .ToListAsync(cancellationToken);

    public void Add(Issue issue) => dbContext.Issues.Add(issue);
}
