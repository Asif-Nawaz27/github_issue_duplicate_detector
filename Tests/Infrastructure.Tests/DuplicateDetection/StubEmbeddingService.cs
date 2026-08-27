using IssueSense.Application.Embeddings;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Infrastructure.Tests.DuplicateDetection;

/// <summary>Always returns the same, caller-supplied vector, for hand-verifiable similarity in tests.</summary>
internal sealed class StubEmbeddingService(EmbeddingVector vector, string modelName = "stub-model") : IEmbeddingService
{
    public Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required to generate an embedding.", nameof(title));

        return Task.FromResult(new EmbeddingResult(vector, modelName));
    }
}
