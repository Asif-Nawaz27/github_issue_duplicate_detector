using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Persistence;
using IssueSense.Application.Tests.Import.Fakes;
using IssueSense.Domain.Entities;
using IssueSense.Domain.Enums;
using IssueSense.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace IssueSense.Application.Tests.DuplicateDetection;

public class DuplicateDetectionServiceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly InMemoryOwnerRepository _ownerRepository = new();
    private readonly InMemoryRepositoryRepository _repositoryRepository;
    private readonly InMemoryIssueEmbeddingRepository _issueEmbeddingRepository = new();
    private readonly InMemoryIssueRepository _issueRepository;

    public DuplicateDetectionServiceTests()
    {
        _repositoryRepository = new InMemoryRepositoryRepository(_ownerRepository);
        _issueRepository = new InMemoryIssueRepository(_issueEmbeddingRepository);
        _issueEmbeddingRepository.IssueRepository = _issueRepository;
    }

    private DuplicateDetectionService CreateSut(EmbeddingVector queryVector, DuplicateDetectionOptions? options = null) =>
        new(
            new StubEmbeddingService(queryVector),
            _repositoryRepository,
            _issueRepository,
            _issueEmbeddingRepository,
            Options.Create(options ?? new DuplicateDetectionOptions()));

    private Repository AddRepository(string owner = "octocat", string name = "hello-world")
    {
        var ownerEntity = _ownerRepository.Owners.SingleOrDefault(o => o.Name == owner);
        if (ownerEntity is null)
        {
            ownerEntity = Owner.Create(owner, BaseTime.UtcDateTime);
            _ownerRepository.Add(ownerEntity);
        }

        var repository = Repository.Create(1, ownerEntity.Id, name, BaseTime);
        _repositoryRepository.Add(repository);

        return repository;
    }

    private Issue AddIssueWithEmbedding(Guid repositoryId, int number, EmbeddingVector vector, string title = "Some issue")
    {
        var issue = Issue.Create(
            repositoryId,
            gitHubIssueId: 1000 + number,
            gitHubIssueNumber: number,
            title: title,
            body: "Body text",
            url: $"https://github.com/octocat/hello-world/issues/{number}",
            createdAt: BaseTime,
            updatedAt: BaseTime);
        _issueRepository.Add(issue);

        _issueEmbeddingRepository.Add(IssueEmbedding.Create(issue.Id, vector, "some-model", BaseTime));

        return issue;
    }

    private static GitHubIssueInfo NewIssue(string title = "App crashes on startup", string? body = "Steps to reproduce...", long id = 9999) =>
        new(id, Number: 999, title, body, IssueState.Open, "https://github.com/octocat/hello-world/issues/999", BaseTime, BaseTime, null, []);

    [Fact]
    public async Task FindDuplicatesAsync_WithRepositoryNotImported_ThrowsRepositoryNotFoundException()
    {
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(() => sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue()));
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithObviousDuplicate_ClassifiesAsHighConfidence()
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([1f, 0f]), title: "App crashes when starting up");
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        var match = Assert.Single(results);
        Assert.Equal(1, match.IssueNumber);
        Assert.Equal(1.0, match.SimilarityScore, precision: 6);
        Assert.Equal(DuplicateConfidence.HighConfidence, match.Confidence);
        Assert.Equal("octocat/hello-world", match.Repository);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithUnrelatedIssue_ExcludesItFromResults()
    {
        var repository = AddRepository();
        // Orthogonal vectors -> cosine similarity 0.0, well below the default 0.50 threshold.
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([0f, 1f]), title: "Add dark mode support");
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithSimilarButDifferentIssue_ClassifiesAsPossible()
    {
        var repository = AddRepository();
        // [0.8, 0.6] has cosine similarity 0.8 with [1,0]: between Possible (0.75) and HighConfidence (0.90).
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([0.8f, 0.6f]), title: "App fails to launch sometimes");
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        var match = Assert.Single(results);
        Assert.Equal(0.8, match.SimilarityScore, precision: 6);
        Assert.Equal(DuplicateConfidence.Possible, match.Confidence);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithEmptyIssueBody_StillReturnsCandidates()
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([1f, 0f]));
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue(body: null))).Candidates;

        Assert.Single(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithVeryShortIssueTitle_StillReturnsCandidates()
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([1f, 0f]), title: "Crash");
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue(title: "Bug", body: null))).Candidates;

        Assert.Single(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_ExcludesTheIssueItselfWhenAlreadyImported()
    {
        var repository = AddRepository();
        var selfVector = EmbeddingVector.Create([1f, 0f]);
        var self = AddIssueWithEmbedding(repository.Id, 999, selfVector, title: "The issue being checked");
        // self.GitHubIssueId is 1000 + 999 = 1999; make the "new issue" reference that same GitHub id.
        var newIssue = NewIssue(id: self.GitHubIssueId);
        var sut = CreateSut(selfVector);

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", newIssue)).Candidates;

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithMultipleCandidates_OrdersBySimilarityDescendingAndRespectsTopN()
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([0.6f, 0.8f]), title: "Somewhat similar");   // similarity 0.6
        AddIssueWithEmbedding(repository.Id, 2, EmbeddingVector.Create([1f, 0f]), title: "Identical");              // similarity 1.0
        AddIssueWithEmbedding(repository.Id, 3, EmbeddingVector.Create([0.8f, 0.6f]), title: "Quite similar");      // similarity 0.8
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]), new DuplicateDetectionOptions { TopN = 2, MinimumSimilarityThreshold = 0.0 });

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        Assert.Equal(2, results.Count);
        Assert.Equal(2, results[0].IssueNumber); // similarity 1.0, highest
        Assert.Equal(3, results[1].IssueNumber); // similarity 0.8, second highest
    }

    [Theory]
    [InlineData(0.0, 3)]
    [InlineData(0.65, 2)]
    [InlineData(0.85, 1)]
    [InlineData(1.01, 0)]
    public async Task FindDuplicatesAsync_RespectsConfiguredMinimumSimilarityThreshold(double threshold, int expectedCount)
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([0.6f, 0.8f])); // similarity 0.6
        AddIssueWithEmbedding(repository.Id, 2, EmbeddingVector.Create([0.8f, 0.6f])); // similarity 0.8
        AddIssueWithEmbedding(repository.Id, 3, EmbeddingVector.Create([1f, 0f]));     // similarity 1.0
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]), new DuplicateDetectionOptions { TopN = 10, MinimumSimilarityThreshold = threshold });

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        Assert.Equal(expectedCount, results.Count);
    }

    [Fact]
    public async Task FindDuplicatesAsync_ReturnsIssueStateAndUrlAndTitle()
    {
        var repository = AddRepository();
        var issue = AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([1f, 0f]), title: "Exact title match");
        issue.Close(BaseTime.AddDays(1));
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var results = (await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue())).Candidates;

        var match = Assert.Single(results);
        Assert.Equal("Exact title match", match.Title);
        Assert.Equal(issue.Url, match.Url);
        Assert.Equal(IssueState.Closed, match.State);
    }

    [Fact]
    public async Task FindDuplicatesAsync_ReturnsEmbeddingModelAndThresholdUsed()
    {
        AddRepository();
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]), new DuplicateDetectionOptions { MinimumSimilarityThreshold = 0.42 });

        var result = await sut.FindDuplicatesAsync("octocat", "hello-world", NewIssue());

        Assert.Equal("stub-model", result.EmbeddingModelUsed);
        Assert.Equal(0.42, result.SimilarityThresholdUsed);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithTitleAndBodyOnly_DoesNotExcludeAnyExistingIssue()
    {
        var repository = AddRepository();
        AddIssueWithEmbedding(repository.Id, 1, EmbeddingVector.Create([1f, 0f]), title: "Identical text");
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        var result = await sut.FindDuplicatesAsync("octocat", "hello-world", "App crashes on startup", "Steps to reproduce...");

        Assert.Single(result.Candidates);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithTitleAndBodyOnly_AndRepositoryNotImported_ThrowsRepositoryNotFoundException()
    {
        var sut = CreateSut(EmbeddingVector.Create([1f, 0f]));

        await Assert.ThrowsAsync<RepositoryNotFoundException>(
            () => sut.FindDuplicatesAsync("octocat", "hello-world", "title", "body"));
    }
}
