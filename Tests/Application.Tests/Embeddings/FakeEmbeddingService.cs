using IssueSense.Application.Embeddings;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Application.Tests.Embeddings;

/// <summary>
/// Deterministic, dependency-free stand-in for a real embedding provider: same text always
/// produces the same vector, different text (almost always) produces a different one, and no
/// model files or network calls are involved. Intended for tests of anything that consumes
/// IEmbeddingService without needing a real model loaded.
/// </summary>
public sealed class FakeEmbeddingService(int dimensions = 8, string modelName = "fake-embedding-model") : IEmbeddingService
{
    public List<(string Title, string? Body)> Requests { get; } = [];

    public Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required to generate an embedding.", nameof(title));

        Requests.Add((title, body));

        var text = string.IsNullOrWhiteSpace(body) ? title : $"{title}\n\n{body}";
        var vector = HashToVector(text, dimensions);

        return Task.FromResult(new EmbeddingResult(EmbeddingVector.Create(vector), modelName));
    }

    private static float[] HashToVector(string text, int dimensions)
    {
        var seed = text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));
        var random = new Random(seed);

        var vector = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
            vector[i] = (float)(random.NextDouble() * 2.0 - 1.0);

        return vector;
    }
}
