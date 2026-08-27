using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Application.Persistence;

public interface IIssueEmbeddingRepository
{
    void Add(IssueEmbedding embedding);

    /// <summary>
    /// Finds the issues in the given repository whose embeddings are most similar to
    /// <paramref name="queryVector"/>, ordered by similarity descending. Only candidates at or
    /// above <paramref name="minimumSimilarity"/> are returned, and <paramref name="excludeIssueId"/>
    /// (the issue being checked, if it's already stored) is never included in the results.
    /// </summary>
    Task<IReadOnlyList<SimilarIssueMatch>> FindSimilarAsync(
        Guid repositoryId,
        EmbeddingVector queryVector,
        int topN,
        double minimumSimilarity,
        Guid? excludeIssueId = null,
        CancellationToken cancellationToken = default);
}
