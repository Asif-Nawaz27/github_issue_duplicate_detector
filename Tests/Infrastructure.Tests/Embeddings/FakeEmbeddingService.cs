using IssueSense.Application.Embeddings;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Infrastructure.Tests.Embeddings;

// Defaults to 384 dimensions to match the issue_embeddings.Vector column
// (IssueEmbeddingConfiguration.EmbeddingDimensions) that every test here writes into.
internal sealed class FakeEmbeddingService(int dimensions = 384, string modelName = "fake-embedding-model") : IEmbeddingService
{
    public HashSet<string> TitlesToFail { get; } = [];

    public Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required to generate an embedding.", nameof(title));

        if (TitlesToFail.Contains(title))
            throw new InvalidOperationException($"Simulated embedding provider failure for '{title}'.");

        var text = string.IsNullOrWhiteSpace(body) ? title : $"{title}\n\n{body}";
        var seed = text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));
        var random = new Random(seed);

        var vector = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
            vector[i] = (float)(random.NextDouble() * 2.0 - 1.0);

        return Task.FromResult(new EmbeddingResult(EmbeddingVector.Create(vector), modelName));
    }
}
