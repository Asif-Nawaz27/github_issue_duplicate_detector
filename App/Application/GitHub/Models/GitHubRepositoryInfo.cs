namespace IssueSense.Application.GitHub.Models;

public sealed record GitHubRepositoryInfo(
    long Id,
    string Owner,
    string Name,
    string HtmlUrl);
