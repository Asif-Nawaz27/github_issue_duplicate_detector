namespace IssueSense.Application.GitHub;

public sealed class GitHubRateLimitExceededException : Exception
{
    public DateTimeOffset ResetAt { get; }

    public GitHubRateLimitExceededException(DateTimeOffset resetAt)
        : base($"GitHub API rate limit exceeded. Resets at {resetAt:O}.")
    {
        ResetAt = resetAt;
    }
}
