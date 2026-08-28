using System.Globalization;
using IssueSense.Application.DuplicateDetection;

namespace IssueSense.Infrastructure.GitHub.Webhooks;

internal static class DuplicateCommentFormatter
{
    /// <summary>
    /// Always present in a posted comment's body, regardless of how the visible template is
    /// customized — this is what makes repeated-webhook idempotency detection reliable even if
    /// someone changes DuplicateCommentOptions.Template to something unrecognizable.
    /// </summary>
    public const string IdempotencyMarker = "<!-- issuesense:duplicate-comment -->";

    public static string Format(string template, DuplicateCandidateMatch candidate)
    {
        var rendered = template
            .Replace("{ExistingIssueUrl}", candidate.Url, StringComparison.Ordinal)
            .Replace("{ExistingIssueNumber}", candidate.IssueNumber.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{ExistingIssueTitle}", candidate.Title, StringComparison.Ordinal)
            .Replace("{Confidence}", candidate.Confidence.ToString(), StringComparison.Ordinal)
            .Replace("{SimilarityScore}", candidate.SimilarityScore.ToString("F2", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{SimilarityPercent}", (candidate.SimilarityScore * 100).ToString("F0", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        return $"{IdempotencyMarker}\n{rendered}";
    }
}
