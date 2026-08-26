using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

internal sealed class GitHubRepositoryDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("owner")]
    public GitHubUserDto Owner { get; init; } = new();
}
