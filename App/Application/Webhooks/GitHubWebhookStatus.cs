namespace IssueSense.Application.Webhooks;

/// <summary>What GitHubWebhookService decided to do with a delivery; the caller maps this to an HTTP status.</summary>
public enum GitHubWebhookStatus
{
    /// <summary>Signature missing/malformed/didn't match the payload. Caller should return 401.</summary>
    InvalidSignature,

    /// <summary>Payload wasn't valid JSON. Caller should return 400.</summary>
    MalformedPayload,

    /// <summary>GitHub's connectivity-check event. Caller should return 200.</summary>
    Ping,

    /// <summary>An event type other than "issues" (or "issues" but not a usable issue payload). Caller should return 200.</summary>
    Ignored,

    /// <summary>Verified, parsed, and handed to duplicate detection. Caller should return 200.</summary>
    Processed,
}
