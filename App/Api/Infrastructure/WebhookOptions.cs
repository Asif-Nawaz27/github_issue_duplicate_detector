namespace IssueSense.Api.Infrastructure;

/// <summary>
/// The HTTP header names GitHub's webhook delivery uses, read from configuration so they aren't
/// hardcoded in <c>WebhooksController</c>. There's no real reason to expect GitHub to ever
/// change these, but a controller reading them from <see cref="AppSettings"/> costs nothing and
/// keeps every configurable value going through the same one path.
/// </summary>
public sealed class WebhookOptions
{
    public string SignatureHeader { get; set; } = "X-Hub-Signature-256";

    public string EventHeader { get; set; } = "X-GitHub-Event";
}
