using System.Text.Json.Serialization;

namespace IssueSense.Infrastructure.GitHub.Dtos;

internal sealed class GitHubLabelDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;
}
