namespace IssueSense.Application.GitHub.Models;

/// <summary>Maps directly to GitHub's `state` query parameter on the list-issues endpoint.</summary>
public enum GitHubIssueStateFilter
{
    Open,
    Closed,
    All
}
