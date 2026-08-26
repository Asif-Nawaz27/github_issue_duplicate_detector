using IssueSense.Application.Embeddings;
using IssueSense.Domain.Entities;
using IssueSense.Infrastructure.Persistence;
using IssueSense.Infrastructure.Persistence.Repositories;
using IssueSense.Infrastructure.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueSense.Infrastructure.Tests.Embeddings;

/// <summary>
/// Exercises embedding generation against a real Postgres+pgvector container, with a fake
/// IEmbeddingService standing in for the model. Requires Docker.
/// </summary>
public sealed class EmbeddingGenerationServiceIntegrationTests(PostgresContainerFixture fixture) : IClassFixture<PostgresContainerFixture>
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static EmbeddingGenerationService CreateSut(IssueSenseDbContext dbContext, FakeEmbeddingService embeddingService) =>
        new(
            embeddingService,
            new RepositoryRepository(dbContext),
            new IssueRepository(dbContext),
            new IssueEmbeddingRepository(dbContext),
            new UnitOfWork(dbContext),
            NullLogger<EmbeddingGenerationService>.Instance);

    private static async Task<(string RepoName, Guid RepositoryId, List<Guid> IssueIds)> SeedRepositoryWithIssuesAsync(
        IssueSenseDbContext dbContext, int issueCount)
    {
        var repoName = $"repo-{Guid.NewGuid():N}";
        var repository = Repository.Create(Random.Shared.NextInt64(1, int.MaxValue), "octocat", repoName, BaseTime);
        dbContext.Repositories.Add(repository);

        var issueIds = new List<Guid>();
        for (var i = 1; i <= issueCount; i++)
        {
            var issue = Issue.Create(
                repository.Id,
                gitHubIssueId: Random.Shared.NextInt64(1, int.MaxValue),
                gitHubIssueNumber: i,
                title: $"Issue {i} for {repoName}",
                body: "Some body text",
                url: $"https://github.com/octocat/{repoName}/issues/{i}",
                createdAt: BaseTime,
                updatedAt: BaseTime);

            dbContext.Issues.Add(issue);
            issueIds.Add(issue.Id);
        }

        await dbContext.SaveChangesAsync();

        return (repoName, repository.Id, issueIds);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_PersistsEmbeddingsWithModelNameAndTimestamp()
    {
        string repoName;
        await using (var dbContext = fixture.CreateDbContext())
        {
            (repoName, _, _) = await SeedRepositoryWithIssuesAsync(dbContext, issueCount: 3);
        }

        var embeddingService = new FakeEmbeddingService(modelName: "fake-integration-model");
        EmbeddingGenerationResult result;
        var before = DateTimeOffset.UtcNow;
        await using (var dbContext = fixture.CreateDbContext())
        {
            result = await CreateSut(dbContext, embeddingService).GenerateEmbeddingsAsync("octocat", repoName);
        }

        Assert.Equal(3, result.TotalIssuesProcessed);
        Assert.Equal(3, result.EmbeddingsGenerated);
        Assert.Equal(0, result.IssuesSkipped);
        Assert.Equal(0, result.Failures);

        await using var verifyContext = fixture.CreateDbContext();
        var repository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        var embeddings = await verifyContext.IssueEmbeddings
            .Join(verifyContext.Issues, e => e.IssueId, i => i.Id, (e, i) => new { Embedding = e, Issue = i })
            .Where(x => x.Issue.RepositoryId == repository.Id)
            .Select(x => x.Embedding)
            .ToListAsync();

        Assert.Equal(3, embeddings.Count);
        Assert.All(embeddings, e =>
        {
            Assert.Equal("fake-integration-model", e.ModelName);
            Assert.Equal(384, e.Vector.Dimension);
            Assert.True(e.CreatedAt >= before);
        });
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_RunTwice_SecondRunSkipsEverythingAndAddsNoNewRows()
    {
        string repoName;
        await using (var dbContext = fixture.CreateDbContext())
        {
            (repoName, _, _) = await SeedRepositoryWithIssuesAsync(dbContext, issueCount: 2);
        }

        var embeddingService = new FakeEmbeddingService();
        await using (var dbContext = fixture.CreateDbContext())
            await CreateSut(dbContext, embeddingService).GenerateEmbeddingsAsync("octocat", repoName);

        EmbeddingGenerationResult secondResult;
        await using (var dbContext = fixture.CreateDbContext())
            secondResult = await CreateSut(dbContext, embeddingService).GenerateEmbeddingsAsync("octocat", repoName);

        Assert.Equal(0, secondResult.EmbeddingsGenerated);
        Assert.Equal(2, secondResult.IssuesSkipped);

        await using var verifyContext = fixture.CreateDbContext();
        var repository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        var embeddingCount = await verifyContext.IssueEmbeddings
            .Join(verifyContext.Issues, e => e.IssueId, i => i.Id, (e, i) => i)
            .CountAsync(i => i.RepositoryId == repository.Id);

        Assert.Equal(2, embeddingCount);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WhenOneIssueFails_StillPersistsTheSuccessfulOnes()
    {
        string repoName;
        await using (var dbContext = fixture.CreateDbContext())
        {
            (repoName, _, _) = await SeedRepositoryWithIssuesAsync(dbContext, issueCount: 3);
        }

        var failingTitle = $"Issue 2 for {repoName}";
        var embeddingService = new FakeEmbeddingService();
        embeddingService.TitlesToFail.Add(failingTitle);

        EmbeddingGenerationResult result;
        await using (var dbContext = fixture.CreateDbContext())
        {
            result = await CreateSut(dbContext, embeddingService).GenerateEmbeddingsAsync("octocat", repoName);
        }

        Assert.Equal(3, result.TotalIssuesProcessed);
        Assert.Equal(2, result.EmbeddingsGenerated);
        Assert.Equal(1, result.Failures);

        await using var verifyContext = fixture.CreateDbContext();
        var repository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        var issuesWithEmbeddings = await verifyContext.IssueEmbeddings
            .Join(verifyContext.Issues, e => e.IssueId, i => i.Id, (e, i) => i)
            .Where(i => i.RepositoryId == repository.Id)
            .Select(i => i.Title)
            .ToListAsync();

        Assert.Equal(2, issuesWithEmbeddings.Count);
        Assert.DoesNotContain(failingTitle, issuesWithEmbeddings);
    }
}
