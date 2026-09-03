using IssueSense.Api.Infrastructure;
using IssueSense.Application.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace IssueSense.Api.Controllers;

[Route("api/webhooks")]
public sealed class WebhooksController(IGitHubWebhookService webhookService, AppSettings appSettings, ILoggerFactory loggerFactory)
    : BaseController(loggerFactory)
{
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
        var signatureHeader = GetHeader(appSettings.Webhook.SignatureHeader);
        var eventType = GetHeader(appSettings.Webhook.EventHeader);

        var result = await webhookService.ProcessAsync(payload, signatureHeader, eventType, cancellationToken);

        return result.Status switch
        {
            GitHubWebhookStatus.InvalidSignature => Unauthorized(new { error = result.Message }),
            GitHubWebhookStatus.MalformedPayload => BadRequest(new { error = result.Message }),
            GitHubWebhookStatus.Ping or GitHubWebhookStatus.Ignored => Ok(new { message = result.Message }),
            GitHubWebhookStatus.Processed => Ok(new { processed = result.Processed, reason = result.Message, duplicatesFound = result.DuplicatesFound }),
            _ => Ok(),
        };
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
}
