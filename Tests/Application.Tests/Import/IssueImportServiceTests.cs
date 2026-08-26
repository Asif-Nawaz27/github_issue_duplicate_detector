using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Import;
using IssueSense.Application.Tests.Import.Fakes;
using IssueSense.Domain.Enums;

namespace IssueSense.Application.Tests.Import;

public class IssueImportServiceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeGitHubService _gitHubService = new();
    private readonly InMemoryRepositoryRepository _repositoryRepository = new();
    private readonly InMemoryIssueRepository _issueRepository = new();
    private readonly InMemoryUnitOfWork _unitOfWork = new();

    private IssueImportService CreateSut() =>
        new(_gitHubService, _repositoryRepository, _issueRepository, _unitOfWork);

    private static GitHubIssueInfo CreateGitHubIssue(
        int number,
        string title = "Some issue",
        IssueState state = IssueState.Open,
        IReadOnlyList<string>? labels = null,
        DateTimeOffset? updatedAt = null,
        DateTimeOffset? closedAt = null) =>
        new(
            Id: 1000 + number,
            Number: number,
            Title: title,
            Body: "Body text",
            State: state,
            HtmlUrl: $"https://github.com/octocat/hello-world/issues/{number}",
            CreatedAt: BaseTime,
            UpdatedAt: updatedAt ?? BaseTime,
            ClosedAt: closedAt,
            Labels: labels ?? []);

    [Fact]
    public async Task ImportAsync_WithNoExistingRepository_CreatesRepository()
    {
        var sut = CreateSut();

        await sut.ImportAsync("octocat", "hello-world");

        var repository = Assert.Single(_repositoryRepository.Repositories);
        Assert.Equal("octocat", repository.Owner);
        Assert.Equal("hello-world", repository.Name);
        Assert.Equal(1, repository.GitHubRepositoryId);
    }

    [Fact]
    public async Task ImportAsync_WithExistingRepository_DoesNotCreateDuplicate()
    {
        var sut = CreateSut();
        await sut.ImportAsync("octocat", "hello-world");

        await sut.ImportAsync("octocat", "hello-world");

        Assert.Single(_repositoryRepository.Repositories);
    }

    [Fact]
    public async Task ImportAsync_WithNewIssues_CreatesThemAndReturnsAccurateCounts()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1), CreateGitHubIssue(2), CreateGitHubIssue(3)];
        var sut = CreateSut();

        var result = await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(3, result.IssuesDiscovered);
        Assert.Equal(3, result.IssuesCreated);
        Assert.Equal(0, result.IssuesUpdated);
        Assert.Equal(0, result.IssuesSkipped);
        Assert.Equal(3, _issueRepository.Issues.Count);
    }

    [Fact]
    public async Task ImportAsync_StoresLabelsOnCreatedIssue()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1, labels: ["bug", "help wanted"])];
        var sut = CreateSut();

        await sut.ImportAsync("octocat", "hello-world");

        var issue = Assert.Single(_issueRepository.Issues);
        Assert.Equal(["bug", "help wanted"], issue.Labels);
    }

    [Fact]
    public async Task ImportAsync_WithClosedIssueOnGitHub_CreatesIssueAlreadyClosed()
    {
        var closedAt = BaseTime.AddDays(2);
        _gitHubService.Issues = [CreateGitHubIssue(1, state: IssueState.Closed, closedAt: closedAt, updatedAt: closedAt)];
        var sut = CreateSut();

        await sut.ImportAsync("octocat", "hello-world");

        var issue = Assert.Single(_issueRepository.Issues);
        Assert.Equal(IssueState.Closed, issue.State);
        Assert.Equal(closedAt, issue.ClosedAt);
    }

    [Fact]
    public async Task ImportAsync_RunTwiceWithUnchangedData_IsIdempotentAndSkipsSecondTime()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1), CreateGitHubIssue(2)];
        var sut = CreateSut();
        await sut.ImportAsync("octocat", "hello-world");

        var result = await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(2, result.IssuesDiscovered);
        Assert.Equal(0, result.IssuesCreated);
        Assert.Equal(0, result.IssuesUpdated);
        Assert.Equal(2, result.IssuesSkipped);
        Assert.Equal(2, _issueRepository.Issues.Count);
        Assert.Single(_repositoryRepository.Repositories);
    }

    [Fact]
    public async Task ImportAsync_WhenIssueTitleChangedOnGitHub_UpdatesExistingIssue()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1, title: "Original title")];
        var sut = CreateSut();
        await sut.ImportAsync("octocat", "hello-world");

        _gitHubService.Issues = [CreateGitHubIssue(1, title: "Updated title", updatedAt: BaseTime.AddDays(1))];
        var result = await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(0, result.IssuesCreated);
        Assert.Equal(1, result.IssuesUpdated);
        Assert.Equal(0, result.IssuesSkipped);
        var issue = Assert.Single(_issueRepository.Issues);
        Assert.Equal("Updated title", issue.Title);
    }

    [Fact]
    public async Task ImportAsync_WhenIssueClosedOnGitHub_TransitionsExistingIssueToClosed()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1, state: IssueState.Open)];
        var sut = CreateSut();
        await sut.ImportAsync("octocat", "hello-world");

        var closedAt = BaseTime.AddDays(3);
        _gitHubService.Issues = [CreateGitHubIssue(1, state: IssueState.Closed, closedAt: closedAt, updatedAt: closedAt)];
        var result = await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(1, result.IssuesUpdated);
        var issue = Assert.Single(_issueRepository.Issues);
        Assert.Equal(IssueState.Closed, issue.State);
        Assert.Equal(closedAt, issue.ClosedAt);
    }

    [Fact]
    public async Task ImportAsync_WhenIssueLabelsChangedOnGitHub_UpdatesLabels()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1, labels: ["bug"])];
        var sut = CreateSut();
        await sut.ImportAsync("octocat", "hello-world");

        _gitHubService.Issues = [CreateGitHubIssue(1, labels: ["bug", "urgent"], updatedAt: BaseTime.AddDays(1))];
        var result = await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(1, result.IssuesUpdated);
        var issue = Assert.Single(_issueRepository.Issues);
        Assert.Equal(["bug", "urgent"], issue.Labels);
    }

    [Fact]
    public async Task ImportAsync_CallsSaveChangesExactlyOncePerImport()
    {
        _gitHubService.Issues = [CreateGitHubIssue(1), CreateGitHubIssue(2)];
        var sut = CreateSut();

        await sut.ImportAsync("octocat", "hello-world");

        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }
}
