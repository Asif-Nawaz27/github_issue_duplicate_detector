using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryIssueEmbeddingRepository : IIssueEmbeddingRepository
{
    // Set after construction to avoid a constructor cycle with InMemoryIssueRepository (which
    // optionally takes this class too, for its own GetWithoutEmbeddingAsync). Only needed by
    // tests that call FindSimilarAsync.
    public InMemoryIssueRepository? IssueRepository { get; set; }

    public List<IssueEmbedding> Embeddings { get; } = [];

    public void Add(IssueEmbedding embedding) => Embeddings.Add(embedding);

    public Task<IReadOnlyList<SimilarIssueMatch>> FindSimilarAsync(
        Guid repositoryId,
        EmbeddingVector queryVector,
        int topN,
        double minimumSimilarity,
        Guid? excludeIssueId = null,
        CancellationToken cancellationToken = default)
    {
        if (IssueRepository is null)
            throw new InvalidOperationException($"{nameof(IssueRepository)} must be set before calling {nameof(FindSimilarAsync)}.");

        var issuesById = IssueRepository.Issues
            .Where(i => i.RepositoryId == repositoryId)
            .ToDictionary(i => i.Id);

        var matches = Embeddings
            .Where(e => issuesById.ContainsKey(e.IssueId) && e.IssueId != excludeIssueId)
            .Select(e => new SimilarIssueMatch(issuesById[e.IssueId], CosineSimilarity(queryVector.Values, e.Vector.Values)))
            .Where(m => m.SimilarityScore >= minimumSimilarity)
            .OrderByDescending(m => m.SimilarityScore)
            .Take(topN)
            .ToList();

        return Task.FromResult<IReadOnlyList<SimilarIssueMatch>>(matches);
    }

    private static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
