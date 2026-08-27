using IssueSense.Application.Embeddings;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Application.Tests.DuplicateDetection;

/// <summary>
/// Always returns the same, caller-supplied vector, so tests can construct exact, hand-verifiable
/// cosine similarities between the "new issue" and stored embeddings — something the hash-based
/// FakeEmbeddingService can't give precise control over.
/// </summary>
internal sealed class StubEmbeddingService(EmbeddingVector vector, string modelName = "stub-model") : IEmbeddingService
{
    public Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required to generate an embedding.", nameof(title));

        return Task.FromResult(new EmbeddingResult(vector, modelName));
    }
}
