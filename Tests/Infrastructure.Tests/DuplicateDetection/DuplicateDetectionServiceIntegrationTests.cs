using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using IssueSense.Domain.Entities;
using IssueSense.Domain.Enums;
using IssueSense.Domain.ValueObjects;
using IssueSense.Infrastructure.Persistence;
using IssueSense.Infrastructure.Persistence.Repositories;
using IssueSense.Infrastructure.Tests.TestFixtures;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.Tests.DuplicateDetection;

/// <summary>
/// Exercises the real pgvector cosine-similarity SQL query (IssueEmbeddingRepository.FindSimilarAsync)
/// against a live Postgres container — the in-memory fake used by the unit tests never touches
/// that SQL, so this is the only place it's actually proven to work. Requires Docker.
/// </summary>
public sealed class DuplicateDetectionServiceIntegrationTests(PostgresContainerFixture fixture) : IClassFixture<PostgresContainerFixture>
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Vectors padded to 384 dims (matching the real column) with the interesting values in the
    // first two components and zeros elsewhere; cosine similarity reduces to the same 2D math
    // used in the unit tests, since the zero components don't affect the dot product or norms.
    private static EmbeddingVector CreateVector(float first, float second)
    {
        var values = new float[384];
        values[0] = first;
        values[1] = second;

        return EmbeddingVector.Create(values);
    }

    private static DuplicateDetectionService CreateSut(
        IssueSenseDbContext dbContext, EmbeddingVector queryVector, DuplicateDetectionOptions? options = null) =>
        new(
            new StubEmbeddingService(queryVector),
            new RepositoryRepository(dbContext),
            new IssueRepository(dbContext),
            new IssueEmbeddingRepository(dbContext),
            Options.Create(options ?? new DuplicateDetectionOptions()));

    private static async Task<(string RepoName, Repository Repository)> SeedRepositoryAsync(IssueSenseDbContext dbContext)
    {
        var repoName = $"repo-{Guid.NewGuid():N}";
        var repository = Repository.Create(Random.Shared.NextInt64(1, int.MaxValue), "octocat", repoName, BaseTime);
        dbContext.Repositories.Add(repository);
        await dbContext.SaveChangesAsync();

        return (repoName, repository);
    }

    private static async Task<Issue> SeedIssueWithEmbeddingAsync(
        IssueSenseDbContext dbContext, Guid repositoryId, int number, EmbeddingVector vector, string title = "Some issue")
    {
        var issue = Issue.Create(
            repositoryId,
            gitHubIssueId: Random.Shared.NextInt64(1, int.MaxValue),
            gitHubIssueNumber: number,
            title: title,
            body: "Body text",
            url: $"https://github.com/octocat/repo/issues/{number}",
            createdAt: BaseTime,
            updatedAt: BaseTime);
        dbContext.Issues.Add(issue);

        dbContext.IssueEmbeddings.Add(IssueEmbedding.Create(issue.Id, vector, "some-model", BaseTime));
        await dbContext.SaveChangesAsync();

        return issue;
    }

    private static GitHubIssueInfo NewIssue(string title = "App crashes on startup", string? body = "Steps to reproduce...", long id = -1) =>
        new(id, Number: 999, title, body, IssueState.Open, "https://github.com/octocat/repo/issues/999", BaseTime, BaseTime, null, []);

    [Fact]
    public async Task FindDuplicatesAsync_WithObviousDuplicate_ClassifiesAsHighConfidence()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(1f, 0f), title: "App crashes when starting up");
        var sut = CreateSut(dbContext, CreateVector(1f, 0f));

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue())).Candidates;

        var match = Assert.Single(results);
        Assert.Equal(1, match.IssueNumber);
        Assert.Equal(1.0, match.SimilarityScore, precision: 4);
        Assert.Equal(DuplicateConfidence.HighConfidence, match.Confidence);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithUnrelatedIssue_ExcludesItFromResults()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(0f, 1f), title: "Add dark mode support");
        var sut = CreateSut(dbContext, CreateVector(1f, 0f));

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue())).Candidates;

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithSimilarButDifferentIssue_ClassifiesAsPossible()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(0.8f, 0.6f), title: "App fails to launch sometimes");
        var sut = CreateSut(dbContext, CreateVector(1f, 0f));

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue())).Candidates;

        var match = Assert.Single(results);
        Assert.Equal(0.8, match.SimilarityScore, precision: 3);
        Assert.Equal(DuplicateConfidence.Possible, match.Confidence);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithEmptyIssueBody_StillReturnsCandidates()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(1f, 0f));
        var sut = CreateSut(dbContext, CreateVector(1f, 0f));

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue(body: null))).Candidates;

        Assert.Single(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithVeryShortIssueTitle_StillReturnsCandidates()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(1f, 0f), title: "Crash");
        var sut = CreateSut(dbContext, CreateVector(1f, 0f));

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue(title: "Bug", body: null))).Candidates;

        Assert.Single(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_ExcludesTheIssueItselfWhenAlreadyImported()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        var vector = CreateVector(1f, 0f);
        var self = await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 999, vector, title: "The issue being checked");
        var sut = CreateSut(dbContext, vector);

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue(id: self.GitHubIssueId))).Candidates;

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithMultipleCandidates_OrdersBySimilarityDescendingAndRespectsTopN()
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(0.6f, 0.8f), title: "Somewhat similar");
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 2, CreateVector(1f, 0f), title: "Identical");
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 3, CreateVector(0.8f, 0.6f), title: "Quite similar");
        var sut = CreateSut(dbContext, CreateVector(1f, 0f), new DuplicateDetectionOptions { TopN = 2, MinimumSimilarityThreshold = 0.0 });

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue())).Candidates;

        Assert.Equal(2, results.Count);
        Assert.Equal(2, results[0].IssueNumber);
        Assert.Equal(3, results[1].IssueNumber);
    }

    [Theory]
    [InlineData(0.0, 3)]
    [InlineData(0.65, 2)]
    [InlineData(0.85, 1)]
    [InlineData(1.01, 0)]
    public async Task FindDuplicatesAsync_RespectsConfiguredMinimumSimilarityThreshold(double threshold, int expectedCount)
    {
        await using var dbContext = fixture.CreateDbContext();
        var (repoName, repository) = await SeedRepositoryAsync(dbContext);
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 1, CreateVector(0.6f, 0.8f));
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 2, CreateVector(0.8f, 0.6f));
        await SeedIssueWithEmbeddingAsync(dbContext, repository.Id, 3, CreateVector(1f, 0f));
        var sut = CreateSut(dbContext, CreateVector(1f, 0f), new DuplicateDetectionOptions { TopN = 10, MinimumSimilarityThreshold = threshold });

        var results = (await sut.FindDuplicatesAsync("octocat", repoName, NewIssue())).Candidates;

        Assert.Equal(expectedCount, results.Count);
    }
}
