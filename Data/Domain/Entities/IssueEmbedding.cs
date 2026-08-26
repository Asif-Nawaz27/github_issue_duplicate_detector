using IssueSense.Domain.Common;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Entities;

public sealed class IssueEmbedding : Entity
{
    public Guid IssueId { get; }

    public EmbeddingVector Vector { get; }

    public string ModelName { get; }

    public DateTimeOffset CreatedAt { get; }

    private IssueEmbedding(Guid id, Guid issueId, EmbeddingVector vector, string modelName, DateTimeOffset createdAt)
        : base(id)
    {
        IssueId = issueId;
        Vector = vector;
        ModelName = modelName;
        CreatedAt = createdAt;
    }

    public static IssueEmbedding Create(Guid issueId, EmbeddingVector vector, string modelName, DateTimeOffset createdAt)
    {
        if (issueId == Guid.Empty)
            throw new ArgumentException("Embedding must belong to an issue.", nameof(issueId));
        ArgumentNullException.ThrowIfNull(vector);
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Embedding model name is required.", nameof(modelName));

        return new IssueEmbedding(Guid.NewGuid(), issueId, vector, modelName.Trim(), createdAt);
    }

#pragma warning disable CS8618 // Required by EF Core for materialization; properties are set via reflection.
    private IssueEmbedding()
    {
    }
#pragma warning restore CS8618
}
