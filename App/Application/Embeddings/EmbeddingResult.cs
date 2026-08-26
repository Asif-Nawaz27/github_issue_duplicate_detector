using IssueSense.Domain.ValueObjects;

namespace IssueSense.Application.Embeddings;

/// <summary>
/// The model name travels with the vector so callers can persist it directly onto an
/// IssueEmbedding — the embedding is meaningless for comparison without knowing which
/// model produced it, since different models produce vectors that aren't comparable.
/// </summary>
public sealed record EmbeddingResult(EmbeddingVector Vector, string ModelName);
