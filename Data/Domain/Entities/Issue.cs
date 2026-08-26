using IssueSense.Domain.Common;
using IssueSense.Domain.Enums;

namespace IssueSense.Domain.Entities;

public sealed class Issue : Entity
{
    private readonly List<string> _labels = [];

    // Referenced by id rather than a navigation property: Issue and Repository are
    // separate aggregate roots, each persisted and loaded independently.
    public Guid RepositoryId { get; }

    public long GitHubIssueId { get; }

    public int GitHubIssueNumber { get; }

    public string Title { get; private set; }

    public string? Body { get; private set; }

    public IssueState State { get; private set; }

    public string Url { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyList<string> Labels => _labels;

    private Issue(
        Guid id,
        Guid repositoryId,
        long gitHubIssueId,
        int gitHubIssueNumber,
        string title,
        string? body,
        string url,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<string> labels)
        : base(id)
    {
        RepositoryId = repositoryId;
        GitHubIssueId = gitHubIssueId;
        GitHubIssueNumber = gitHubIssueNumber;
        Title = title;
        Body = body;
        State = IssueState.Open;
        Url = url;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _labels.AddRange(NormalizeLabels(labels));
    }

    public static Issue Create(
        Guid repositoryId,
        long gitHubIssueId,
        int gitHubIssueNumber,
        string title,
        string? body,
        string url,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<string>? labels = null)
    {
        if (repositoryId == Guid.Empty)
            throw new ArgumentException("Issue must belong to a repository.", nameof(repositoryId));
        if (gitHubIssueId <= 0)
            throw new ArgumentOutOfRangeException(nameof(gitHubIssueId), gitHubIssueId, "GitHub issue id must be positive.");
        if (gitHubIssueNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(gitHubIssueNumber), gitHubIssueNumber, "GitHub issue number must be positive.");
        ValidateTitle(title);
        ValidateUrl(url);
        if (updatedAt < createdAt)
            throw new ArgumentException("Issue cannot be updated before it was created.", nameof(updatedAt));

        return new Issue(
            Guid.NewGuid(),
            repositoryId,
            gitHubIssueId,
            gitHubIssueNumber,
            title.Trim(),
            body,
            url,
            createdAt,
            updatedAt,
            labels ?? []);
    }

#pragma warning disable CS8618 // Required by EF Core for materialization; properties are set via reflection.
    private Issue()
    {
    }
#pragma warning restore CS8618

    public void UpdateContent(string title, string? body, DateTimeOffset updatedAt)
    {
        ValidateTitle(title);
        if (updatedAt < CreatedAt)
            throw new ArgumentException("Issue cannot be updated before it was created.", nameof(updatedAt));

        Title = title.Trim();
        Body = body;
        UpdatedAt = updatedAt;
    }

    public void ReplaceLabels(IEnumerable<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        _labels.Clear();
        _labels.AddRange(NormalizeLabels(labels));
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (closedAt < CreatedAt)
            throw new ArgumentException("Issue cannot be closed before it was created.", nameof(closedAt));

        State = IssueState.Closed;
        ClosedAt = closedAt;
        if (closedAt > UpdatedAt)
            UpdatedAt = closedAt;
    }

    public void Reopen(DateTimeOffset updatedAt)
    {
        if (updatedAt < CreatedAt)
            throw new ArgumentException("Issue cannot be reopened before it was created.", nameof(updatedAt));

        State = IssueState.Open;
        ClosedAt = null;
        UpdatedAt = updatedAt;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Issue title is required.", nameof(title));
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("Issue URL must be a valid absolute URL.", nameof(url));
    }

    private static IEnumerable<string> NormalizeLabels(IEnumerable<string> labels) =>
        labels
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
