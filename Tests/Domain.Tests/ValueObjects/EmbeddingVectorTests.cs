using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Tests.ValueObjects;

public class EmbeddingVectorTests
{
    [Fact]
    public void Create_WithValues_SetsDimensionAndValues()
    {
        var vector = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);

        Assert.Equal(3, vector.Dimension);
        Assert.Equal([0.1f, 0.2f, 0.3f], vector.Values);
    }

    [Fact]
    public void Create_WithEmptyCollection_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmbeddingVector.Create([]));
    }

    [Fact]
    public void Create_WithNullCollection_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EmbeddingVector.Create(null!));
    }

    [Fact]
    public void Equals_WithSameValuesInSameOrder_ReturnsTrue()
    {
        var first = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);
        var second = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        var first = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);
        var second = EmbeddingVector.Create([0.9f, 0.2f, 0.3f]);

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_WithDifferentDimensions_ReturnsFalse()
    {
        var first = EmbeddingVector.Create([0.1f, 0.2f]);
        var second = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);

        Assert.NotEqual(first, second);
    }
}
