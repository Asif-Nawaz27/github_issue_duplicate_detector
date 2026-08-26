namespace IssueSense.Application.Embeddings;

/// <summary>
/// Generates and stores embeddings for a repository's issues that don't have one yet. A plain
/// Application service with no web-specific dependencies, so it can be called from an API
/// endpoint today and from a background worker later without any change to this contract.
/// </summary>
public interface IEmbeddingGenerationService
{
    Task<EmbeddingGenerationResult> GenerateEmbeddingsAsync(string owner, string name, CancellationToken cancellationToken = default);
}
