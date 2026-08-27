namespace IssueSense.Application.Webhooks;

/// <summary>Verifies that a webhook payload was genuinely sent by GitHub and hasn't been tampered with.</summary>
public interface IGitHubWebhookSignatureVerifier
{
    bool IsValid(byte[] payload, string? signatureHeader);
}
