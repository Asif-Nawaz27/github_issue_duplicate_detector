namespace IssueSense.Infrastructure.GitHub;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    // Must end with a trailing slash: HttpClient.BaseAddress + a relative request URI only
    // appends correctly when the base ends in '/' and the relative path has no leading '/'.
    public string BaseUrl { get; set; } = "https://api.github.com/";

    public string AccessToken { get; set; } = string.Empty;

    public string UserAgent { get; set; } = "IssueSense";

    /// <summary>Shared secret configured on the GitHub webhook, used to verify inbound payload signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
