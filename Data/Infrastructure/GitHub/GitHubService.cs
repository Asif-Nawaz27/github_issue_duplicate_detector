using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;
using IssueSense.Infrastructure.GitHub.Dtos;

namespace IssueSense.Infrastructure.GitHub;

internal sealed class GitHubService(HttpClient httpClient) : IGitHubService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GitHubRepositoryInfo> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        var requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, $"Repository '{owner}/{name}'");

        var dto = await response.Content.ReadFromJsonAsync<GitHubRepositoryDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty repository response.");

        return GitHubMapper.ToRepositoryInfo(dto);
    }

    public async IAsyncEnumerable<GitHubIssueInfo> GetIssuesAsync(
        string owner,
        string name,
        GitHubIssueStateFilter state = GitHubIssueStateFilter.All,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stateValue = state.ToString().ToLowerInvariant();
        string? requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/issues?state={stateValue}&per_page=100";

        while (requestUri is not null)
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            await EnsureSuccessOrThrowAsync(response, $"Issues for '{owner}/{name}'");

            var dtos = await response.Content.ReadFromJsonAsync<List<GitHubIssueDto>>(JsonOptions, cancellationToken) ?? [];

            foreach (var dto in dtos)
            {
                // GitHub's issues endpoint also returns pull requests; skip those.
                if (dto.PullRequest is not null)
                    continue;

                yield return GitHubMapper.ToIssueInfo(dto);
            }

            requestUri = GetNextPageUrl(response);
        }
    }

    public async Task<GitHubIssueInfo> GetIssueAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default)
    {
        var requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/issues/{issueNumber}";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, $"Issue #{issueNumber} in '{owner}/{name}'");

        var dto = await response.Content.ReadFromJsonAsync<GitHubIssueDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty issue response.");

        return GitHubMapper.ToIssueInfo(dto);
    }

    public async Task<IReadOnlyList<GitHubLabel>> GetIssueLabelsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default)
    {
        var requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/issues/{issueNumber}/labels";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, $"Labels for issue #{issueNumber} in '{owner}/{name}'");

        var dtos = await response.Content.ReadFromJsonAsync<List<GitHubLabelDto>>(JsonOptions, cancellationToken) ?? [];

        return dtos.Select(dto => new GitHubLabel(dto.Name, dto.Color)).ToList();
    }

    public async Task<IReadOnlyList<GitHubComment>> GetIssueCommentsAsync(string owner, string name, int issueNumber, CancellationToken cancellationToken = default)
    {
        var requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/issues/{issueNumber}/comments?per_page=100";
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, $"Comments for issue #{issueNumber} in '{owner}/{name}'");

        var dtos = await response.Content.ReadFromJsonAsync<List<GitHubCommentDto>>(JsonOptions, cancellationToken) ?? [];

        return dtos.Select(GitHubMapper.ToComment).ToList();
    }

    public async Task<GitHubComment> PostIssueCommentAsync(string owner, string name, int issueNumber, string body, CancellationToken cancellationToken = default)
    {
        var requestUri = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/issues/{issueNumber}/comments";
        using var response = await httpClient.PostAsJsonAsync(requestUri, new { body }, JsonOptions, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, $"Posting comment on issue #{issueNumber} in '{owner}/{name}'");

        var dto = await response.Content.ReadFromJsonAsync<GitHubCommentDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty comment response.");

        return GitHubMapper.ToComment(dto);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string resourceDescription)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new GitHubNotFoundException($"{resourceDescription} was not found on GitHub.");

        // The rate-limit handler already intercepts 403s caused by hitting a rate limit; a 403
        // that reaches here is a genuine permission problem (e.g. token lacks issues:write).
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new GitHubForbiddenException($"GitHub denied permission for: {resourceDescription}.");

        response.EnsureSuccessStatusCode();
    }

    private static string? GetNextPageUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values))
            return null;

        var linkHeader = values.FirstOrDefault();
        if (string.IsNullOrEmpty(linkHeader))
            return null;

        foreach (var link in linkHeader.Split(','))
        {
            var segments = link.Split(';', StringSplitOptions.TrimEntries);
            if (segments.Length < 2 || segments[1] != "rel=\"next\"")
                continue;

            return segments[0].Trim('<', '>');
        }

        return null;
    }
}
