using IssueSense.Domain.Entities;

namespace IssueSense.Domain.Tests.Entities;

public class RepositoryTests
{
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndDefaultsToActive()
    {
        var createdAt = DateTimeOffset.UtcNow;

        var repository = Repository.Create(12345, "octocat", "hello-world", createdAt);

        Assert.NotEqual(Guid.Empty, repository.Id);
        Assert.Equal(12345, repository.GitHubRepositoryId);
        Assert.Equal("octocat", repository.Owner);
        Assert.Equal("hello-world", repository.Name);
        Assert.Equal("octocat/hello-world", repository.FullName);
        Assert.True(repository.IsActive);
        Assert.Equal(createdAt, repository.CreatedAt);
    }

    [Fact]
    public void Create_TrimsOwnerAndName()
    {
        var repository = Repository.Create(1, "  octocat  ", "  hello-world  ", DateTimeOffset.UtcNow);

        Assert.Equal("octocat", repository.Owner);
        Assert.Equal("hello-world", repository.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveGitHubRepositoryId_Throws(long gitHubRepositoryId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Repository.Create(gitHubRepositoryId, "octocat", "hello-world", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingOwner_Throws(string? owner)
    {
        Assert.Throws<ArgumentException>(
            () => Repository.Create(1, owner!, "hello-world", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(
            () => Repository.Create(1, "octocat", name!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var repository = Repository.Create(1, "octocat", "hello-world", DateTimeOffset.UtcNow);

        repository.Deactivate();

        Assert.False(repository.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var repository = Repository.Create(1, "octocat", "hello-world", DateTimeOffset.UtcNow);
        repository.Deactivate();

        repository.Activate();

        Assert.True(repository.IsActive);
    }

    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var repository = Repository.Create(1, "octocat", "hello-world", createdAt);

        // Entity equality is identity-based (by Id), not structural — two distinct
        // Create() calls produce different ids and must not compare equal.
        var otherWithDifferentId = Repository.Create(1, "octocat", "hello-world", createdAt);

        Assert.NotEqual(repository, otherWithDifferentId);
        Assert.Equal(repository, repository);
    }
}
