using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class IssueEmbeddingRepository(IssueSenseDbContext dbContext) : IIssueEmbeddingRepository
{
    public void Add(IssueEmbedding embedding) => dbContext.IssueEmbeddings.Add(embedding);

    public async Task<IReadOnlyList<SimilarIssueMatch>> FindSimilarAsync(
        Guid repositoryId,
        EmbeddingVector queryVector,
        int topN,
        double minimumSimilarity,
        Guid? excludeIssueId = null,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(queryVector.Values.ToArray());

        // Cosine similarity isn't expressible through the EmbeddingVector <-> Pgvector.Vector
        // value converter in LINQ (pgvector's distance operators only translate when the query
        // targets Pgvector.Vector directly), so this goes straight to SQL for the ranked search.
        var rows = await dbContext.Database
            .SqlQuery<SimilarIssueRow>($"""
                SELECT ie."IssueId" AS "IssueId", 1 - (ie."Vector" <=> {vector}) AS "Similarity"
                FROM issue_embeddings ie
                JOIN issues i ON i."Id" = ie."IssueId"
                WHERE i."RepositoryId" = {repositoryId}
                  AND 1 - (ie."Vector" <=> {vector}) >= {minimumSimilarity}
                ORDER BY ie."Vector" <=> {vector}
                LIMIT {(excludeIssueId.HasValue ? topN + 1 : topN)}
                """)
            .ToListAsync(cancellationToken);

        if (excludeIssueId.HasValue)
            rows = rows.Where(r => r.IssueId != excludeIssueId.Value).Take(topN).ToList();

        if (rows.Count == 0)
            return [];

        var issueIds = rows.Select(r => r.IssueId).ToList();
        var issuesById = await dbContext.Issues
            .Where(i => issueIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        return rows
            .Where(r => issuesById.ContainsKey(r.IssueId))
            .Select(r => new SimilarIssueMatch(issuesById[r.IssueId], r.Similarity))
            .ToList();
    }

    private sealed record SimilarIssueRow(Guid IssueId, double Similarity);
}
