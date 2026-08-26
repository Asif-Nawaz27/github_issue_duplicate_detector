using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryIssueEmbeddingRepository : IIssueEmbeddingRepository
{
    public List<IssueEmbedding> Embeddings { get; } = [];

    public void Add(IssueEmbedding embedding) => Embeddings.Add(embedding);
}
