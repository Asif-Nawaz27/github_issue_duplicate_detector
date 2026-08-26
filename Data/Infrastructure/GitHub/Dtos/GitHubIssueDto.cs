using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

internal sealed class GitHubIssueDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("number")]
    public int Number { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("closed_at")]
    public DateTimeOffset? ClosedAt { get; init; }

    [JsonPropertyName("labels")]
    public List<GitHubLabelDto> Labels { get; init; } = [];

    // GitHub's issues endpoint also returns pull requests; only present (non-null) on those.
    [JsonPropertyName("pull_request")]
    public object? PullRequest { get; init; }
}
