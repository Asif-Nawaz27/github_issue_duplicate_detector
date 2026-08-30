using System.Text.Json;
using IssueSense.Application.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace IssueSense.Api.Controllers;

public sealed partial class WebhooksController(
    IGitHubWebhookSignatureVerifier signatureVerifier,
    IGitHubWebhookParser webhookParser,
    IGitHubIssueWebhookHandler webhookHandler,
    ILoggerFactory loggerFactory) : BaseController(loggerFactory)
{
    private const string SignatureHeaderName = "X-Hub-Signature-256";
    private const string EventHeaderName = "X-GitHub-Event";

    /// <summary>
    /// Receives GitHub "issues" webhook deliveries. Verifies the payload signature, runs
    /// duplicate detection for newly-opened issues, and logs the result. Read-only: this never
    /// creates, closes, or modifies anything on GitHub or in this system.
    /// </summary>
    /// <response code="200">The delivery was authentic; see the body for whether it was acted on.</response>
    /// <response code="400">The payload was authentic but not valid JSON / not a recognizable issue event.</response>
    /// <response code="401">The signature was missing, malformed, or did not match the payload.</response>
    [HttpPost("github")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveGitHubWebhook(CancellationToken cancellationToken)
    {
        var payload = await ReadBodyAsync(Request.Body, cancellationToken);

        var signatureHeader = GetHeader(SignatureHeaderName);
        if (!signatureVerifier.IsValid(payload, signatureHeader))
        {
            LogInvalidSignature(Logger);
            return Unauthorized(new { error = "Invalid or missing webhook signature." });
        }

        var eventType = GetHeader(EventHeaderName);

        if (eventType == "ping")
        {
            LogPingReceived(Logger);
            return Ok(new { message = "pong" });
        }

        if (eventType != "issues")
        {
            LogIgnoredEventType(Logger, eventType ?? "(missing)");
            return Ok(new { message = $"Event type '{eventType}' is not handled." });
        }

        if (!IsWellFormedJson(payload))
        {
            LogMalformedPayload(Logger);
            return BadRequest(new { error = "Payload is not valid JSON." });
        }

        var webhookEvent = webhookParser.ParseIssueEvent(payload);
        if (webhookEvent is null)
        {
            LogUnparseablePayload(Logger);
            return Ok(new { message = "Payload ignored: not a usable issue event (e.g. a pull request, or missing data)." });
        }

        var result = await webhookHandler.HandleAsync(webhookEvent, cancellationToken);

        LogWebhookHandled(Logger, webhookEvent.Repository.Owner, webhookEvent.Repository.Name, webhookEvent.Issue.Number, result.Processed, result.Reason);

        return Ok(new { processed = result.Processed, reason = result.Reason, duplicatesFound = result.DuplicatesFound });
    }

    // No [FromBody] binding: the signature must be verified against the exact raw bytes GitHub
    // sent, before anything is deserialized or trusted.
    private static async Task<byte[]> ReadBodyAsync(Stream requestBody, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await requestBody.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private string? GetHeader(string name) =>
        Request.Headers.TryGetValue(name, out StringValues value) && !StringValues.IsNullOrEmpty(value)
            ? value.ToString()
            : null;

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
    private static partial void LogInvalidSignature(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received GitHub webhook ping")]
    private static partial void LogPingReceived(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring GitHub webhook event type '{EventType}'")]
    private static partial void LogIgnoredEventType(ILogger logger, string eventType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring GitHub webhook payload: not a usable issue event")]
    private static partial void LogUnparseablePayload(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected GitHub webhook delivery: payload is not valid JSON")]
    private static partial void LogMalformedPayload(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GitHub webhook for {Owner}/{Repository} issue #{IssueNumber}: processed={Processed}, reason={Reason}")]
    private static partial void LogWebhookHandled(ILogger logger, string owner, string repository, int issueNumber, bool processed, string reason);
}
