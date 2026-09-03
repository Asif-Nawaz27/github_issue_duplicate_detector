using IssueSense.Application.Webhooks;

namespace IssueSense.Application.Tests.Webhooks.Fakes;

internal sealed class FakeGitHubWebhookSignatureVerifier : IGitHubWebhookSignatureVerifier
{
    public bool IsValidResult { get; set; } = true;

    public bool IsValid(byte[] payload, string? signatureHeader) => IsValidResult;
}
