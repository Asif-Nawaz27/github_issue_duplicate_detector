using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Tests.ValueObjects;

public class SimilarityScoreTests
{
    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(0.8734)]
    [InlineData(1.0)]
    public void Create_WithValueInRange_SetsValue(double value)
    {
        var score = SimilarityScore.Create(value);

        Assert.Equal(value, score.Value);
    }

    [Theory]
    [InlineData(1.0001)]
    [InlineData(-1.0001)]
    [InlineData(2.0)]
    public void Create_WithValueOutOfRange_Throws(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimilarityScore.Create(value));
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var first = SimilarityScore.Create(0.5);
        var second = SimilarityScore.Create(0.5);

        Assert.Equal(first, second);
    }
}
