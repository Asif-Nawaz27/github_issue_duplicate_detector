using IssueSense.Infrastructure.Embeddings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.Tests.Embeddings;

/// <summary>
/// Exercises the real ONNX model end to end (downloads it into the local cache on first run,
/// then runs actual inference). Requires network access the first time; fully offline after.
/// </summary>
public sealed class LocalEmbeddingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly LocalEmbeddingService _sut;

    public LocalEmbeddingServiceTests()
    {
        var services = new ServiceCollection();
        services.AddOptions<LocalEmbeddingOptions>();
        services.AddHttpClient(LocalEmbeddingService.HttpClientName);
        services.AddSingleton<LocalEmbeddingService>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = _serviceProvider.GetRequiredService<LocalEmbeddingService>();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ProducesVectorWithConfiguredDimensionsAndModelName()
    {
        var result = await _sut.GenerateEmbeddingAsync("App crashes on startup", "Steps to reproduce...");

        Assert.Equal(384, result.Vector.Dimension);
        Assert.Equal("all-MiniLM-L6-v2", result.ModelName);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullBody_DoesNotThrow()
    {
        var result = await _sut.GenerateEmbeddingAsync("A title with no body", null);

        Assert.Equal(384, result.Vector.Dimension);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateEmbeddingAsync_WithMissingTitle_ThrowsArgumentException(string? title)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GenerateEmbeddingAsync(title!, "body"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithExtremelyLongBody_DoesNotThrowAndStillProducesFixedSizeVector()
    {
        var longBody = string.Join(" ", Enumerable.Repeat("word", 20_000));

        var result = await _sut.GenerateEmbeddingAsync("Title", longBody);

        Assert.Equal(384, result.Vector.Dimension);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SameTextTwice_ProducesIdenticalVector()
    {
        var first = await _sut.GenerateEmbeddingAsync("Login button does nothing", "Clicking login has no effect.");
        var second = await _sut.GenerateEmbeddingAsync("Login button does nothing", "Clicking login has no effect.");

        Assert.Equal(first.Vector, second.Vector);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SimilarIssues_AreMoreSimilarThanUnrelatedIssues()
    {
        var original = await _sut.GenerateEmbeddingAsync(
            "App crashes on startup",
            "The application crashes immediately after launching on Windows 11.");
        var duplicate = await _sut.GenerateEmbeddingAsync(
            "Application crashes when opening",
            "When I launch the app on Windows 11 it crashes right away.");
        var unrelated = await _sut.GenerateEmbeddingAsync(
            "Add dark mode support",
            "It would be nice to have a dark theme option in settings.");

        var similarityToDuplicate = CosineSimilarity(original.Vector.Values, duplicate.Vector.Values);
        var similarityToUnrelated = CosineSimilarity(original.Vector.Values, unrelated.Vector.Values);

        Assert.True(
            similarityToDuplicate > similarityToUnrelated,
            $"Expected the near-duplicate crash report ({similarityToDuplicate:F4}) to score more similar " +
            $"than the unrelated feature request ({similarityToUnrelated:F4}).");
    }

    private static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public void Dispose() => _serviceProvider.Dispose();
}
