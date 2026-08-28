namespace IssueSense.Infrastructure.GitHub.Webhooks;

public sealed class DuplicateCommentOptions
{
    public const string SectionName = "DuplicateComment";

    /// <summary>
    /// Supported placeholders: {ExistingIssueUrl}, {ExistingIssueNumber}, {ExistingIssueTitle},
    /// {Confidence}, {SimilarityScore} (0.00-1.00), {SimilarityPercent} (0-100).
    /// </summary>
    public string Template { get; set; } = DefaultTemplate;

    public const string DefaultTemplate =
        "⚠️ **Possible duplicate detected**\n\n" +
        "This issue looks similar to an existing issue: {ExistingIssueUrl}\n\n" +
        "**Confidence:** {Confidence} ({SimilarityPercent}% similar)\n\n" +
        "If this issue is actually different from the one linked above, please reply below to let us know — " +
        "otherwise it may be closed as a duplicate.\n\n" +
        "*This comment was posted automatically by IssueSense.*";
}
