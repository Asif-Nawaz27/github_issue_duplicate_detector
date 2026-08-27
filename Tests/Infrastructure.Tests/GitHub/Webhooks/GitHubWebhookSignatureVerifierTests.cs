using System.Security.Cryptography;
using System.Text;
using IssueSense.Infrastructure.GitHub;
using IssueSense.Infrastructure.GitHub.Webhooks;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.Tests.GitHub.Webhooks;

public class GitHubWebhookSignatureVerifierTests
{
    private const string Secret = "test-webhook-secret";

    private static GitHubWebhookSignatureVerifier CreateSut(string? secret = Secret) =>
        new(Options.Create(new GitHubOptions { WebhookSecret = secret ?? string.Empty }));

    private static string ComputeSignature(byte[] payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(payload);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void IsValid_WithCorrectSignature_ReturnsTrue()
    {
        var payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var signature = ComputeSignature(payload, Secret);
        var sut = CreateSut();

        Assert.True(sut.IsValid(payload, signature));
    }

    [Fact]
    public void IsValid_WithWrongSecret_ReturnsFalse()
    {
        var payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var signature = ComputeSignature(payload, "a-different-secret");
        var sut = CreateSut();

        Assert.False(sut.IsValid(payload, signature));
    }

    [Fact]
    public void IsValid_WithTamperedPayload_ReturnsFalse()
    {
        var originalPayload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var signature = ComputeSignature(originalPayload, Secret);
        var tamperedPayload = Encoding.UTF8.GetBytes("""{"action":"closed"}""");
        var sut = CreateSut();

        Assert.False(sut.IsValid(tamperedPayload, signature));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-signature")]
    [InlineData("sha1=abcdef")]
    [InlineData("sha256=not-valid-hex")]
    public void IsValid_WithMalformedSignatureHeader_ReturnsFalse(string? signatureHeader)
    {
        var payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var sut = CreateSut();

        Assert.False(sut.IsValid(payload, signatureHeader));
    }

    [Fact]
    public void IsValid_WithNoSecretConfigured_ReturnsFalse()
    {
        var payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var signature = ComputeSignature(payload, Secret);
        var sut = CreateSut(secret: null);

        Assert.False(sut.IsValid(payload, signature));
    }

    [Fact]
    public void IsValid_SignatureIsCaseInsensitiveForHexDigits()
    {
        var payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
        var signature = ComputeSignature(payload, Secret).ToUpperInvariant().Replace("SHA256=", "sha256=");
        var sut = CreateSut();

        Assert.True(sut.IsValid(payload, signature));
    }
}
