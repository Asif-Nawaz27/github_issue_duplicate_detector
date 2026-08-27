using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Persistence;
using Microsoft.Extensions.Logging;

namespace IssueSense.Application.Webhooks;

public sealed partial class GitHubIssueWebhookHandler(
    IDuplicateDetectionService duplicateDetectionService,
    IDuplicateNotifier duplicateNotifier,
    ILogger<GitHubIssueWebhookHandler> logger) : IGitHubIssueWebhookHandler
{
    public async Task<WebhookProcessingResult> HandleAsync(GitHubIssueWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        if (webhookEvent.Action != "opened")
        {
            LogIgnoredAction(webhookEvent.Repository.Owner, webhookEvent.Repository.Name, webhookEvent.Action);
            return new WebhookProcessingResult(Processed: false, Reason: $"Ignored: action '{webhookEvent.Action}' is not 'opened'.");
        }

        try
        {
            var result = await duplicateDetectionService.FindDuplicatesAsync(
                webhookEvent.Repository.Owner, webhookEvent.Repository.Name, webhookEvent.Issue, cancellationToken);

            await duplicateNotifier.NotifyAsync(webhookEvent.Repository, webhookEvent.Issue, result, cancellationToken);

            var highestConfidence = result.Candidates.Count == 0
                ? (DuplicateConfidence?)null
                : result.Candidates.Max(c => c.Confidence);

            LogProcessed(
                webhookEvent.Repository.Owner, webhookEvent.Repository.Name, webhookEvent.Issue.Number,
                result.Candidates.Count, highestConfidence);

            return new WebhookProcessingResult(Processed: true, Reason: "Processed.", result.Candidates.Count, highestConfidence);
        }
        catch (RepositoryNotFoundException)
        {
            LogRepositoryNotImported(webhookEvent.Repository.Owner, webhookEvent.Repository.Name);
            return new WebhookProcessingResult(Processed: false, Reason: "Ignored: repository has not been imported yet.");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring webhook for {Owner}/{Repository}: action '{Action}' is not 'opened'")]
    private partial void LogIgnoredAction(string owner, string repository, string action);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processed new issue webhook for {Owner}/{Repository} issue #{IssueNumber}: {CandidateCount} candidate(s) found, highest confidence {HighestConfidence}")]
    private partial void LogProcessed(string owner, string repository, int issueNumber, int candidateCount, DuplicateConfidence? highestConfidence);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring webhook for {Owner}/{Repository}: repository has not been imported yet")]
    private partial void LogRepositoryNotImported(string owner, string repository);
}
