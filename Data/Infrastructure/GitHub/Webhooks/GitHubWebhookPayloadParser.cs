using System.Text.Json;
using IssueSense.Application.Webhooks;
using IssueSense.Infrastructure.GitHub.Dtos;

namespace IssueSense.Infrastructure.GitHub.Webhooks;

internal sealed class GitHubWebhookPayloadParser : IGitHubWebhookParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitHubIssueWebhookEvent? ParseIssueEvent(byte[] payload)
    {
        GitHubIssueEventDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GitHubIssueEventDto>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.Issue is null || dto.Repository is null)
            return null;

        // GitHub only sends the "issues" event for genuine issues, but the issue object's shape
        // is shared with the REST API (where pull_request is set for PR-backed items); guard
        // against it anyway rather than assume the caller already filtered by event type.
        if (dto.Issue.PullRequest is not null)
            return null;

        return new GitHubIssueWebhookEvent(dto.Action, GitHubMapper.ToRepositoryInfo(dto.Repository), GitHubMapper.ToIssueInfo(dto.Issue));
    }
}
