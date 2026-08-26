namespace IssueSense.Application.Embeddings;

/// <summary>
/// Port for turning issue text into a vector embedding. Implemented in Infrastructure against
/// whatever provider is currently configured; nothing here should leak provider-specific
/// concerns (model files, HTTP calls to a paid API, tokenization, etc.).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding for an issue's title and body. Implementations are responsible
    /// for handling empty bodies and truncating text that exceeds what the underlying model
    /// can accept — callers never need to pre-process the text themselves.
    /// </summary>
    Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default);
}
