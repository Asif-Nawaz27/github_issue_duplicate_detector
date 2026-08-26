using IssueSense.Domain.Entities;
using IssueSense.Domain.Enums;

namespace IssueSense.Domain.Tests.Entities;

public class IssueTests
{
    private static readonly Guid RepositoryId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Issue CreateValidIssue(IEnumerable<string>? labels = null) =>
        Issue.Create(
            RepositoryId,
            gitHubIssueId: 1001,
            gitHubIssueNumber: 42,
            title: "App crashes on startup",
            body: "Steps to reproduce...",
            url: "https://github.com/octocat/hello-world/issues/42",
            createdAt: CreatedAt,
            updatedAt: CreatedAt,
            labels: labels);

    [Fact]
    public void Create_WithValidData_SetsPropertiesAndDefaultsToOpen()
    {
        var issue = CreateValidIssue();

        Assert.NotEqual(Guid.Empty, issue.Id);
        Assert.Equal(RepositoryId, issue.RepositoryId);
        Assert.Equal(1001, issue.GitHubIssueId);
        Assert.Equal(42, issue.GitHubIssueNumber);
        Assert.Equal("App crashes on startup", issue.Title);
        Assert.Equal(IssueState.Open, issue.State);
        Assert.Null(issue.ClosedAt);
        Assert.Empty(issue.Labels);
    }

    [Fact]
    public void Create_WithEmptyRepositoryId_Throws()
    {
        Assert.Throws<ArgumentException>(() => Issue.Create(
            Guid.Empty, 1001, 42, "title", null, "https://github.com/o/r/issues/1", CreatedAt, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveGitHubIssueId_Throws(long gitHubIssueId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Issue.Create(
            RepositoryId, gitHubIssueId, 42, "title", null, "https://github.com/o/r/issues/1", CreatedAt, CreatedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveIssueNumber_Throws(int issueNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Issue.Create(
            RepositoryId, 1001, issueNumber, "title", null, "https://github.com/o/r/issues/1", CreatedAt, CreatedAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => Issue.Create(
            RepositoryId, 1001, 42, title!, null, "https://github.com/o/r/issues/1", CreatedAt, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/relative/path")]
    public void Create_WithInvalidUrl_Throws(string url)
    {
        Assert.Throws<ArgumentException>(() => Issue.Create(
            RepositoryId, 1001, 42, "title", null, url, CreatedAt, CreatedAt));
    }

    [Fact]
    public void Create_WithUpdatedAtBeforeCreatedAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => Issue.Create(
            RepositoryId, 1001, 42, "title", null, "https://github.com/o/r/issues/1",
            createdAt: CreatedAt, updatedAt: CreatedAt.AddDays(-1)));
    }

    [Fact]
    public void Create_NormalizesLabels_TrimsAndRemovesDuplicatesAndBlanks()
    {
        var issue = CreateValidIssue(["bug", " bug ", "BUG", "", "  ", "ui"]);

        Assert.Equal(["bug", "ui"], issue.Labels);
    }

    [Fact]
    public void Close_SetsStateClosedAndClosedAt()
    {
        var issue = CreateValidIssue();
        var closedAt = CreatedAt.AddDays(1);

        issue.Close(closedAt);

        Assert.Equal(IssueState.Closed, issue.State);
        Assert.Equal(closedAt, issue.ClosedAt);
        Assert.Equal(closedAt, issue.UpdatedAt);
    }

    [Fact]
    public void Close_BeforeCreatedAt_Throws()
    {
        var issue = CreateValidIssue();

        Assert.Throws<ArgumentException>(() => issue.Close(CreatedAt.AddDays(-1)));
    }

    [Fact]
    public void Reopen_AfterClose_ClearsClosedAtAndSetsStateOpen()
    {
        var issue = CreateValidIssue();
        issue.Close(CreatedAt.AddDays(1));

        issue.Reopen(CreatedAt.AddDays(2));

        Assert.Equal(IssueState.Open, issue.State);
        Assert.Null(issue.ClosedAt);
        Assert.Equal(CreatedAt.AddDays(2), issue.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_WithValidData_UpdatesTitleBodyAndUpdatedAt()
    {
        var issue = CreateValidIssue();
        var updatedAt = CreatedAt.AddHours(1);

        issue.UpdateContent("New title", "New body", updatedAt);

        Assert.Equal("New title", issue.Title);
        Assert.Equal("New body", issue.Body);
        Assert.Equal(updatedAt, issue.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_WithBlankTitle_Throws()
    {
        var issue = CreateValidIssue();

        Assert.Throws<ArgumentException>(() => issue.UpdateContent("  ", "body", CreatedAt.AddHours(1)));
    }

    [Fact]
    public void ReplaceLabels_ReplacesExistingLabels()
    {
        var issue = CreateValidIssue(["bug"]);

        issue.ReplaceLabels(["enhancement", "help wanted"]);

        Assert.Equal(["enhancement", "help wanted"], issue.Labels);
    }
}
