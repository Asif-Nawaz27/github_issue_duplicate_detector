using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

internal sealed class GitHubUserDto
{
    [JsonPropertyName("login")]
    public string Login { get; init; } = string.Empty;
}
