using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;

namespace IssueSense.Domain.Tests.Entities;

public class IssueEmbeddingTests
{
    private static readonly Guid IssueId = Guid.NewGuid();
    private static readonly EmbeddingVector Vector = EmbeddingVector.Create([0.1f, 0.2f, 0.3f]);
    private static readonly DateTimeOffset CreatedAt = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var embedding = IssueEmbedding.Create(IssueId, Vector, "text-embedding-3-small", CreatedAt);

        Assert.NotEqual(Guid.Empty, embedding.Id);
        Assert.Equal(IssueId, embedding.IssueId);
        Assert.Equal(Vector, embedding.Vector);
        Assert.Equal("text-embedding-3-small", embedding.ModelName);
        Assert.Equal(CreatedAt, embedding.CreatedAt);
    }

    [Fact]
    public void Create_WithEmptyIssueId_Throws()
    {
        Assert.Throws<ArgumentException>(() => IssueEmbedding.Create(Guid.Empty, Vector, "model", CreatedAt));
    }

    [Fact]
    public void Create_WithNullVector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => IssueEmbedding.Create(IssueId, null!, "model", CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingModelName_Throws(string? modelName)
    {
        Assert.Throws<ArgumentException>(() => IssueEmbedding.Create(IssueId, Vector, modelName!, CreatedAt));
    }
}
