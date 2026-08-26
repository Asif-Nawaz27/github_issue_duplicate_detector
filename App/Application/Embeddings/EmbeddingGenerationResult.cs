namespace IssueSense.Application.Embeddings;

public sealed record EmbeddingGenerationResult(
    int TotalIssuesProcessed,
    int EmbeddingsGenerated,
    int IssuesSkipped,
    int Failures);
