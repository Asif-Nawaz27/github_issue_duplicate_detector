using IssueSense.Domain.Enums;

namespace IssueSense.Application.GitHub.Models;

public sealed record GitHubIssueInfo(
    long Id,
    int Number,
    string Title,
    string? Body,
    IssueState State,
    string HtmlUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<string> Labels);
