using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.GitHub.Webhooks;

/// <summary>
/// Logs every duplicate check, and for high-confidence matches also posts a warning comment on
/// the new issue — never closes or otherwise modifies anything. Idempotent against repeated
/// webhook deliveries: before posting, it checks the issue's existing comments for the marker
/// this notifier always embeds, so a retried/redelivered webhook never produces a second comment.
/// A failure to read or post a comment (rate limit aside, which the HTTP pipeline already
/// handles) is logged and swallowed — the duplicate check itself already succeeded, so a
/// GitHub-side failure here shouldn't make webhook processing look like it failed.
/// </summary>
internal sealed partial class GitHubCommentDuplicateNotifier(
    IGitHubService gitHubService,
    IOptions<DuplicateCommentOptions> options,
    ILogger<GitHubCommentDuplicateNotifier> logger) : IDuplicateNotifier
{
    public async Task NotifyAsync(
        GitHubRepositoryInfo repository,
        GitHubIssueInfo issue,
        DuplicateDetectionResult result,
        CancellationToken cancellationToken = default)
    {
        if (result.Candidates.Count == 0)
        {
            LogNoDuplicatesFound(repository.Owner, repository.Name, issue.Number);
            return;
        }

        var strongest = result.Candidates.MaxBy(c => c.SimilarityScore)!;

        if (strongest.Confidence != DuplicateConfidence.HighConfidence)
        {
            LogPossibleDuplicates(repository.Owner, repository.Name, issue.Number, result.Candidates.Count, strongest.Confidence);
            return;
        }

        await PostDuplicateCommentIfNeededAsync(repository, issue, strongest, cancellationToken);
    }

    private async Task PostDuplicateCommentIfNeededAsync(
        GitHubRepositoryInfo repository, GitHubIssueInfo issue, DuplicateCandidateMatch strongest, CancellationToken cancellationToken)
    {
        try
        {
            var existingComments = await gitHubService.GetIssueCommentsAsync(repository.Owner, repository.Name, issue.Number, cancellationToken);
            if (existingComments.Any(c => c.Body.Contains(DuplicateCommentFormatter.IdempotencyMarker, StringComparison.Ordinal)))
            {
                LogCommentAlreadyPosted(repository.Owner, repository.Name, issue.Number);
                return;
            }

            var commentBody = DuplicateCommentFormatter.Format(options.Value.Template, strongest);
            await gitHubService.PostIssueCommentAsync(repository.Owner, repository.Name, issue.Number, commentBody, cancellationToken);

            LogCommentPosted(repository.Owner, repository.Name, issue.Number, strongest.IssueNumber, strongest.SimilarityScore);
        }
        catch (GitHubForbiddenException ex)
        {
            LogPermissionDenied(ex, repository.Owner, repository.Name, issue.Number);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCommentPostingFailed(ex, repository.Owner, repository.Name, issue.Number);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Owner}/{Repository} issue #{IssueNumber}: no similar issues found")]
    private partial void LogNoDuplicatesFound(string owner, string repository, int issueNumber);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "{Owner}/{Repository} issue #{IssueNumber}: {CandidateCount} possible duplicate(s) found, strongest classified as {Confidence}")]
    private partial void LogPossibleDuplicates(string owner, string repository, int issueNumber, int candidateCount, DuplicateConfidence confidence);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Posted duplicate-warning comment: {Owner}/{Repository} issue #{IssueNumber} matches existing issue " +
            "#{ExistingIssueNumber} (similarity {Similarity:F3})")]
    private partial void LogCommentPosted(string owner, string repository, int issueNumber, int existingIssueNumber, double similarity);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Skipping duplicate comment for {Owner}/{Repository} issue #{IssueNumber}: already posted")]
    private partial void LogCommentAlreadyPosted(string owner, string repository, int issueNumber);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Could not post duplicate comment on {Owner}/{Repository} issue #{IssueNumber}: permission denied by GitHub")]
    private partial void LogPermissionDenied(Exception exception, string owner, string repository, int issueNumber);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to post duplicate comment on {Owner}/{Repository} issue #{IssueNumber}")]
    private partial void LogCommentPostingFailed(Exception exception, string owner, string repository, int issueNumber);
}
