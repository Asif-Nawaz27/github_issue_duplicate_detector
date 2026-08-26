using IssueSense.Domain.Entities;

namespace IssueSense.Application.Persistence;

public interface IIssueEmbeddingRepository
{
    void Add(IssueEmbedding embedding);
}
