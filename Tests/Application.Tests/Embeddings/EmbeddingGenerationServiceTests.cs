using IssueSense.Application.Embeddings;
using IssueSense.Application.Persistence;
using IssueSense.Application.Tests.Import.Fakes;
using IssueSense.Domain.Entities;
using IssueSense.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace IssueSense.Application.Tests.Embeddings;

public class EmbeddingGenerationServiceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeEmbeddingService _embeddingService = new();
    private readonly InMemoryOwnerRepository _ownerRepository = new();
    private readonly InMemoryRepositoryRepository _repositoryRepository;
    private readonly InMemoryIssueEmbeddingRepository _issueEmbeddingRepository = new();
    private readonly InMemoryIssueRepository _issueRepository;
    private readonly InMemoryUnitOfWork _unitOfWork = new();

    public EmbeddingGenerationServiceTests()
    {
        _repositoryRepository = new InMemoryRepositoryRepository(_ownerRepository);
        _issueRepository = new InMemoryIssueRepository(_issueEmbeddingRepository);
    }

    private EmbeddingGenerationService CreateSut() =>
        new(_embeddingService, _repositoryRepository, _issueRepository, _issueEmbeddingRepository, _unitOfWork, NullLogger<EmbeddingGenerationService>.Instance);

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

    private Issue AddIssue(Guid repositoryId, int number, string title = "Some issue")
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

        return issue;
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithRepositoryNotImported_ThrowsRepositoryNotFoundException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<RepositoryNotFoundException>(() => sut.GenerateEmbeddingsAsync("octocat", "hello-world"));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithIssuesLackingEmbeddings_GeneratesForAllOfThem()
    {
        var repository = AddRepository();
        AddIssue(repository.Id, 1);
        AddIssue(repository.Id, 2);
        var sut = CreateSut();

        var result = await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        Assert.Equal(2, result.TotalIssuesProcessed);
        Assert.Equal(2, result.EmbeddingsGenerated);
        Assert.Equal(0, result.IssuesSkipped);
        Assert.Equal(0, result.Failures);
        Assert.Equal(2, _issueEmbeddingRepository.Embeddings.Count);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_StoresModelNameAndTimestampOnEachEmbedding()
    {
        var repository = AddRepository();
        var issue = AddIssue(repository.Id, 1);
        var sut = CreateSut();
        var before = DateTimeOffset.UtcNow;

        await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        var embedding = Assert.Single(_issueEmbeddingRepository.Embeddings);
        Assert.Equal(issue.Id, embedding.IssueId);
        Assert.Equal("fake-embedding-model", embedding.ModelName);
        Assert.True(embedding.CreatedAt >= before);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithIssueAlreadyEmbedded_SkipsItAndDoesNotCallEmbeddingService()
    {
        var repository = AddRepository();
        var alreadyEmbedded = AddIssue(repository.Id, 1, title: "Already embedded");
        AddIssue(repository.Id, 2, title: "Needs embedding");
        _issueEmbeddingRepository.Add(IssueEmbedding.Create(
            alreadyEmbedded.Id,
            EmbeddingVector.Create([0.1f, 0.2f]),
            "some-older-model",
            BaseTime));
        var sut = CreateSut();

        var result = await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        Assert.Equal(2, result.TotalIssuesProcessed);
        Assert.Equal(1, result.EmbeddingsGenerated);
        Assert.Equal(1, result.IssuesSkipped);
        Assert.DoesNotContain(_embeddingService.Requests, r => r.Title == "Already embedded");
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_RunTwice_SecondRunSkipsEverythingAndGeneratesNothingNew()
    {
        var repository = AddRepository();
        AddIssue(repository.Id, 1);
        AddIssue(repository.Id, 2);
        var sut = CreateSut();
        await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        var secondResult = await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        Assert.Equal(2, secondResult.TotalIssuesProcessed);
        Assert.Equal(0, secondResult.EmbeddingsGenerated);
        Assert.Equal(2, secondResult.IssuesSkipped);
        Assert.Equal(2, _issueEmbeddingRepository.Embeddings.Count);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WhenGenerationFailsForOneIssue_CountsFailureAndStillProcessesTheRest()
    {
        var repository = AddRepository();
        AddIssue(repository.Id, 1, title: "Will fail");
        AddIssue(repository.Id, 2, title: "Will succeed");
        _embeddingService.TitlesToFail.Add("Will fail");
        var sut = CreateSut();

        var result = await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        Assert.Equal(2, result.TotalIssuesProcessed);
        Assert.Equal(1, result.EmbeddingsGenerated);
        Assert.Equal(0, result.IssuesSkipped);
        Assert.Equal(1, result.Failures);
        Assert.Single(_issueEmbeddingRepository.Embeddings);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_CallsSaveChangesExactlyOncePerRun()
    {
        var repository = AddRepository();
        AddIssue(repository.Id, 1);
        AddIssue(repository.Id, 2);
        var sut = CreateSut();

        await sut.GenerateEmbeddingsAsync("octocat", "hello-world");

        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }
}
