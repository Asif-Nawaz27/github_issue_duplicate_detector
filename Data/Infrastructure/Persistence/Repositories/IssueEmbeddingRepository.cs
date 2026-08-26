using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class IssueEmbeddingRepository(IssueSenseDbContext dbContext) : IIssueEmbeddingRepository
{
    public void Add(IssueEmbedding embedding) => dbContext.IssueEmbeddings.Add(embedding);
}
