using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IssueSense.Application.Webhooks;

public sealed partial class GitHubWebhookService(
    IGitHubWebhookSignatureVerifier signatureVerifier,
    IGitHubWebhookParser webhookParser,
    IGitHubIssueWebhookHandler webhookHandler,
    ILogger<GitHubWebhookService> logger) : IGitHubWebhookService
{
    public async Task<GitHubWebhookResult> ProcessAsync(
        byte[] payload,
        string? signatureHeader,
        string? eventType,
        CancellationToken cancellationToken = default)
    {
        if (!signatureVerifier.IsValid(payload, signatureHeader))
        {
            LogInvalidSignature();
            return new GitHubWebhookResult(GitHubWebhookStatus.InvalidSignature, "Invalid or missing webhook signature.");
        }

        if (eventType == "ping")
        {
            LogPingReceived();
            return new GitHubWebhookResult(GitHubWebhookStatus.Ping, "pong");
        }

        if (eventType != "issues")
        {
            LogIgnoredEventType(eventType ?? "(missing)");
            return new GitHubWebhookResult(GitHubWebhookStatus.Ignored, $"Event type '{eventType}' is not handled.");
        }

        if (!IsWellFormedJson(payload))
        {
            LogMalformedPayload();
            return new GitHubWebhookResult(GitHubWebhookStatus.MalformedPayload, "Payload is not valid JSON.");
        }

        var webhookEvent = webhookParser.ParseIssueEvent(payload);
        if (webhookEvent is null)
        {
            LogUnparseablePayload();
            return new GitHubWebhookResult(
                GitHubWebhookStatus.Ignored,
                "Payload ignored: not a usable issue event (e.g. a pull request, or missing data).");
        }

        var result = await webhookHandler.HandleAsync(webhookEvent, cancellationToken);

        LogWebhookHandled(
            webhookEvent.Repository.Owner, webhookEvent.Repository.Name, webhookEvent.Issue.Number, result.Processed, result.Reason);

        return new GitHubWebhookResult(GitHubWebhookStatus.Processed, result.Reason, result.Processed, result.DuplicatesFound);
    }

    private static bool IsWellFormedJson(byte[] payload)
    {
        try
        {
            using var _ = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected GitHub webhook delivery with an invalid or missing signature")]
    private partial void LogInvalidSignature();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received GitHub webhook ping")]
    private partial void LogPingReceived();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring GitHub webhook event type '{EventType}'")]
    private partial void LogIgnoredEventType(string eventType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring GitHub webhook payload: not a usable issue event")]
    private partial void LogUnparseablePayload();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected GitHub webhook delivery: payload is not valid JSON")]
    private partial void LogMalformedPayload();

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GitHub webhook for {Owner}/{Repository} issue #{IssueNumber}: processed={Processed}, reason={Reason}")]
    private partial void LogWebhookHandled(string owner, string repository, int issueNumber, bool processed, string reason);
}
