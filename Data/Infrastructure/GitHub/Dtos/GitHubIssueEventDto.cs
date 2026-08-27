using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

/// <summary>Top-level shape of a GitHub "issues" webhook payload.</summary>
internal sealed class GitHubIssueEventDto
{
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("issue")]
    public GitHubIssueDto? Issue { get; init; }

    [JsonPropertyName("repository")]
    public GitHubRepositoryDto? Repository { get; init; }
}
