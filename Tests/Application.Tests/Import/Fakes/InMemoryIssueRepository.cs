using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryIssueRepository(InMemoryIssueEmbeddingRepository? embeddingRepository = null) : IIssueRepository
{
    private readonly InMemoryIssueEmbeddingRepository _embeddingRepository = embeddingRepository ?? new InMemoryIssueEmbeddingRepository();

    public List<Issue> Issues { get; } = [];

    public Task<Issue?> GetByRepositoryIdAndGitHubIssueIdAsync(Guid repositoryId, long gitHubIssueId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Issues.SingleOrDefault(i => i.RepositoryId == repositoryId && i.GitHubIssueId == gitHubIssueId));

    public Task<int> CountByRepositoryIdAsync(Guid repositoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Issues.Count(i => i.RepositoryId == repositoryId));

    public Task<IReadOnlyList<Issue>> GetWithoutEmbeddingAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        var embeddedIssueIds = _embeddingRepository.Embeddings.Select(e => e.IssueId).ToHashSet();
        var result = Issues.Where(i => i.RepositoryId == repositoryId && !embeddedIssueIds.Contains(i.Id)).ToList();

        return Task.FromResult<IReadOnlyList<Issue>>(result);
    }

    public void Add(Issue issue) => Issues.Add(issue);
}
