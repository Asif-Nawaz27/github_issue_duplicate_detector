using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Import;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.Persistence;
using IssueSense.Infrastructure.Persistence.Repositories;
using IssueSense.Infrastructure.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Tests.Import;

/// <summary>
/// Exercises the importer against a real Postgres+pgvector container (via Testcontainers),
/// with a fake IGitHubService standing in for the network call. Requires Docker.
/// </summary>
public sealed class IssueImportServiceIntegrationTests(PostgresContainerFixture fixture) : IClassFixture<PostgresContainerFixture>
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // GitHubIssueId is globally unique (like on real GitHub), and all tests in this class
    // share one database, so each test must use its own id range to avoid cross-test collisions.
    private static GitHubIssueInfo CreateGitHubIssue(
        long issueIdBase,
        int number,
        string title = "Some issue",
        IssueState state = IssueState.Open,
        IReadOnlyList<string>? labels = null,
        DateTimeOffset? updatedAt = null) =>
        new(
            Id: issueIdBase + number,
            Number: number,
            Title: title,
            Body: "Body text",
            State: state,
            HtmlUrl: $"https://github.com/octocat/hello-world/issues/{number}",
            CreatedAt: BaseTime,
            UpdatedAt: updatedAt ?? BaseTime,
            ClosedAt: null,
            Labels: labels ?? []);

    private static IssueImportService CreateSut(IssueSenseDbContext dbContext, FakeGitHubService gitHubService) =>
        new(
            gitHubService,
            new RepositoryRepository(dbContext),
            new IssueRepository(dbContext),
            new OwnerRepository(dbContext),
            new UnitOfWork(dbContext));

    [Fact]
    public async Task ImportAsync_PersistsRepositoryAndIssuesToDatabase()
    {
        var repoName = $"repo-{Guid.NewGuid():N}";
        var issueIdBase = Random.Shared.NextInt64(1, 1_000_000_000);
        var gitHubService = new FakeGitHubService
        {
            Repository = new GitHubRepositoryInfo(42, "octocat", repoName, $"https://github.com/octocat/{repoName}"),
            Issues = [CreateGitHubIssue(issueIdBase, 1, labels: ["bug"]), CreateGitHubIssue(issueIdBase, 2)]
        };

        await using (var dbContext = fixture.CreateDbContext())
        {
            var result = await CreateSut(dbContext, gitHubService).ImportAsync("octocat", repoName);

            Assert.Equal(2, result.IssuesDiscovered);
            Assert.Equal(2, result.IssuesCreated);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var persistedRepository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        var persistedIssues = await verifyContext.Issues
            .Where(i => i.RepositoryId == persistedRepository.Id)
            .OrderBy(i => i.GitHubIssueNumber)
            .ToListAsync();

        Assert.Equal(2, persistedIssues.Count);
        Assert.Equal(["bug"], persistedIssues[0].Labels);
    }

    [Fact]
    public async Task ImportAsync_RunTwice_DoesNotCreateDuplicateRowsInDatabase()
    {
        var repoName = $"repo-{Guid.NewGuid():N}";
        var issueIdBase = Random.Shared.NextInt64(1, 1_000_000_000);
        var gitHubService = new FakeGitHubService
        {
            Repository = new GitHubRepositoryInfo(43, "octocat", repoName, $"https://github.com/octocat/{repoName}"),
            Issues = [CreateGitHubIssue(issueIdBase, 1), CreateGitHubIssue(issueIdBase, 2), CreateGitHubIssue(issueIdBase, 3)]
        };

        await using (var dbContext = fixture.CreateDbContext())
            await CreateSut(dbContext, gitHubService).ImportAsync("octocat", repoName);

        IssueImportResult secondResult;
        await using (var dbContext = fixture.CreateDbContext())
            secondResult = await CreateSut(dbContext, gitHubService).ImportAsync("octocat", repoName);

        Assert.Equal(0, secondResult.IssuesCreated);
        Assert.Equal(3, secondResult.IssuesSkipped);

        await using var verifyContext = fixture.CreateDbContext();
        Assert.Equal(1, await verifyContext.Repositories.CountAsync(r => r.Name == repoName));
        var repository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        Assert.Equal(3, await verifyContext.Issues.CountAsync(i => i.RepositoryId == repository.Id));
    }

    [Fact]
    public async Task ImportAsync_WhenIssueChangedOnGitHub_UpdatesPersistedIssueInPlace()
    {
        var repoName = $"repo-{Guid.NewGuid():N}";
        var issueIdBase = Random.Shared.NextInt64(1, 1_000_000_000);
        var gitHubService = new FakeGitHubService
        {
            Repository = new GitHubRepositoryInfo(44, "octocat", repoName, $"https://github.com/octocat/{repoName}"),
            Issues = [CreateGitHubIssue(issueIdBase, 1, title: "Original title")]
        };

        await using (var dbContext = fixture.CreateDbContext())
            await CreateSut(dbContext, gitHubService).ImportAsync("octocat", repoName);

        gitHubService.Issues = [CreateGitHubIssue(issueIdBase, 1, title: "Updated title", updatedAt: BaseTime.AddDays(1))];

        IssueImportResult result;
        await using (var dbContext = fixture.CreateDbContext())
            result = await CreateSut(dbContext, gitHubService).ImportAsync("octocat", repoName);

        Assert.Equal(1, result.IssuesUpdated);

        await using var verifyContext = fixture.CreateDbContext();
        var repository = await verifyContext.Repositories.SingleAsync(r => r.Name == repoName);
        var issue = await verifyContext.Issues.SingleAsync(i => i.RepositoryId == repository.Id);
        Assert.Equal("Updated title", issue.Title);
    }
}
