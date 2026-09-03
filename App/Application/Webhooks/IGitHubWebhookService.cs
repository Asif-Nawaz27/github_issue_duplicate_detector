namespace IssueSense.Application.Webhooks;

/// <summary>
/// Processes one raw GitHub webhook delivery end to end: verifies the signature, filters to
/// "issues" events, parses the payload, and runs duplicate detection. Takes only the primitives
/// a controller can hand it (raw bytes, header values) — no HTTP types cross this boundary.
/// </summary>
public interface IGitHubWebhookService
{
    Task<GitHubWebhookResult> ProcessAsync(
        byte[] payload,
        string? signatureHeader,
        string? eventType,
        CancellationToken cancellationToken = default);
}
