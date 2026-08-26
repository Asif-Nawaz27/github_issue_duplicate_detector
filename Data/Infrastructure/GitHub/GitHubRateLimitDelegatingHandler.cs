using System.Net;
using IssueSense.Application.GitHub;

namespace IssueSense.Infrastructure.GitHub;

/// <summary>
/// Turns GitHub's rate-limit signal (a 403/429 with rate-limit headers) into a typed
/// exception instead of letting callers hit a generic HTTP error.
/// </summary>
internal sealed class GitHubRateLimitDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests
            && TryGetRateLimitReset(response, out var resetAt))
        {
            throw new GitHubRateLimitExceededException(resetAt);
        }

        return response;
    }

    private static bool TryGetRateLimitReset(HttpResponseMessage response, out DateTimeOffset resetAt)
    {
        if (TryGetHeaderValue(response, "X-RateLimit-Remaining", out var remaining)
            && remaining == "0"
            && TryGetHeaderValue(response, "X-RateLimit-Reset", out var resetSeconds)
            && long.TryParse(resetSeconds, out var unixSeconds))
        {
            resetAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }

        // Secondary/abuse rate limits signal via Retry-After instead of the primary headers.
        if (TryGetHeaderValue(response, "Retry-After", out var retryAfterSeconds)
            && int.TryParse(retryAfterSeconds, out var seconds))
        {
            resetAt = DateTimeOffset.UtcNow.AddSeconds(seconds);
            return true;
        }

        resetAt = default;
        return false;
    }

    private static bool TryGetHeaderValue(HttpResponseMessage response, string name, out string value)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            value = values.FirstOrDefault() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        value = string.Empty;
        return false;
    }
}
