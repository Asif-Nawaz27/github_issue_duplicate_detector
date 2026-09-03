namespace IssueSense.Application.Webhooks;

/// <summary>
/// The outcome of processing one webhook delivery. Plain data — no HTTP types — so the
/// controller (the only layer allowed to know about status codes) decides how to respond.
/// </summary>
public sealed record GitHubWebhookResult(
    GitHubWebhookStatus Status,
    string Message,
    bool Processed = false,
    int DuplicatesFound = 0);
