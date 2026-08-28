using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

internal sealed class GitHubCommentDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("user")]
    public GitHubUserDto? User { get; init; }
}
