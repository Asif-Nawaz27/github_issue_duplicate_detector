using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using Microsoft.Extensions.Logging;

namespace IssueSense.Infrastructure.GitHub.Webhooks;

/// <summary>
/// v1: only logs. A future version can replace this with one that also posts a comment on the
/// new issue for high-confidence matches (via IGitHubService) — nothing calling IDuplicateNotifier
/// needs to change for that swap.
/// </summary>
internal sealed partial class LoggingDuplicateNotifier(ILogger<LoggingDuplicateNotifier> logger) : IDuplicateNotifier
{
    public Task NotifyAsync(
        GitHubRepositoryInfo repository,
        GitHubIssueInfo issue,
        DuplicateDetectionResult result,
        CancellationToken cancellationToken = default)
    {
        if (result.Candidates.Count == 0)
        {
            LogNoDuplicatesFound(repository.Owner, repository.Name, issue.Number);
            return Task.CompletedTask;
        }

        var strongest = result.Candidates.MaxBy(c => c.SimilarityScore)!;

        if (strongest.Confidence == DuplicateConfidence.HighConfidence)
        {
            LogHighConfidenceDuplicate(repository.Owner, repository.Name, issue.Number, strongest.IssueNumber, strongest.SimilarityScore);
        }
        else
        {
            LogPossibleDuplicates(repository.Owner, repository.Name, issue.Number, result.Candidates.Count, strongest.Confidence);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "High-confidence duplicate: {Owner}/{Repository} issue #{IssueNumber} closely matches existing " +
            "issue #{ExistingIssueNumber} (similarity {Similarity:F3}). A comment would be posted here in a future version.")]
    private partial void LogHighConfidenceDuplicate(string owner, string repository, int issueNumber, int existingIssueNumber, double similarity);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "{Owner}/{Repository} issue #{IssueNumber}: {CandidateCount} possible duplicate(s) found, strongest classified as {Confidence}")]
    private partial void LogPossibleDuplicates(string owner, string repository, int issueNumber, int candidateCount, DuplicateConfidence confidence);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Owner}/{Repository} issue #{IssueNumber}: no similar issues found")]
    private partial void LogNoDuplicatesFound(string owner, string repository, int issueNumber);
}
