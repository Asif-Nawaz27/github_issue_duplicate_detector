using IssueSense.Application.GitHub.Models;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub.Dtos;

namespace IssueSense.Infrastructure.GitHub;

/// <summary>Shared GitHub wire-DTO to Application-model mapping, used by both the REST client and webhook parsing.</summary>
internal static class GitHubMapper
{
    public static GitHubRepositoryInfo ToRepositoryInfo(GitHubRepositoryDto dto) =>
        new(dto.Id, dto.Owner.Login, dto.Name, dto.HtmlUrl);

    public static GitHubIssueInfo ToIssueInfo(GitHubIssueDto dto) =>
        new(
            dto.Id,
            dto.Number,
            dto.Title,
            dto.Body,
            dto.State.Equals("closed", StringComparison.OrdinalIgnoreCase) ? IssueState.Closed : IssueState.Open,
            dto.HtmlUrl,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.ClosedAt,
            dto.Labels.Select(label => label.Name).ToList());
}
