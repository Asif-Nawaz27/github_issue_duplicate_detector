using System.Security.Cryptography;
using System.Text;
using IssueSense.Application.Webhooks;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.GitHub.Webhooks;

internal sealed class GitHubWebhookSignatureVerifier(IOptions<GitHubOptions> options) : IGitHubWebhookSignatureVerifier
{
    private const string SignaturePrefix = "sha256=";

    public bool IsValid(byte[] payload, string? signatureHeader)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith(SignaturePrefix, StringComparison.Ordinal))
            return false;

        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrEmpty(secret))
            return false; // Fail closed if no secret is configured, rather than accept anything.

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(signatureHeader.AsSpan(SignaturePrefix.Length));
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedBytes = hmac.ComputeHash(payload);

        return CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
    }
}
