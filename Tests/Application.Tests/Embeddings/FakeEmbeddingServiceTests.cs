namespace IssueSense.Application.Tests.Embeddings;

public class FakeEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_WithSameInput_ReturnsSameVectorDeterministically()
    {
        var sut = new FakeEmbeddingService();

        var first = await sut.GenerateEmbeddingAsync("App crashes on startup", "Steps to reproduce...");
        var second = await sut.GenerateEmbeddingAsync("App crashes on startup", "Steps to reproduce...");

        Assert.Equal(first.Vector, second.Vector);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithDifferentInput_ReturnsDifferentVector()
    {
        var sut = new FakeEmbeddingService();

        var first = await sut.GenerateEmbeddingAsync("App crashes on startup", null);
        var second = await sut.GenerateEmbeddingAsync("Dark mode request", null);

        Assert.NotEqual(first.Vector, second.Vector);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_UsesConfiguredDimensionsAndModelName()
    {
        var sut = new FakeEmbeddingService(dimensions: 16, modelName: "test-model-v1");

        var result = await sut.GenerateEmbeddingAsync("title", "body");

        Assert.Equal(16, result.Vector.Dimension);
        Assert.Equal("test-model-v1", result.ModelName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateEmbeddingAsync_WithMissingTitle_Throws(string? title)
    {
        var sut = new FakeEmbeddingService();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GenerateEmbeddingAsync(title!, "body"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullOrEmptyBody_DoesNotThrow()
    {
        var sut = new FakeEmbeddingService();

        var result = await sut.GenerateEmbeddingAsync("title only", null);

        Assert.Equal(8, result.Vector.Dimension);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_RecordsEachRequest()
    {
        var sut = new FakeEmbeddingService();

        await sut.GenerateEmbeddingAsync("title one", "body one");
        await sut.GenerateEmbeddingAsync("title two", null);

        Assert.Equal(2, sut.Requests.Count);
        Assert.Equal(("title one", "body one"), sut.Requests[0]);
        Assert.Equal(("title two", (string?)null), sut.Requests[1]);
    }
}
