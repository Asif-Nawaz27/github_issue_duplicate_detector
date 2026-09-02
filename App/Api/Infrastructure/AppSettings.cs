using IssueSense.Application.DuplicateDetection;
using IssueSense.Infrastructure.GitHub.Webhooks;

namespace IssueSense.Api.Infrastructure;

/// <summary>
/// Strongly-typed mirror of the "AppSettings" section in appsettings.json — read once here
/// instead of scattering raw <c>configuration.GetSection("...")</c> calls around the solution.
/// Bound via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> for anything that
/// wants to inject the whole thing, and also used directly by
/// <see cref="MiddlewareExtension"/> at startup for pieces (like CORS) that are needed before
/// the DI container is built.
/// </summary>
/// <remarks>
/// Settings that already have their own focused options type and DI wiring for a reason other
/// than "nobody's centralized it yet" stay outside this class — the GitHub integration
/// (<c>GitHubOptions</c>, top-level "GitHub" section, holding credentials) and the database
/// connection string (<c>ConnectionStrings:Postgres</c>) aren't part of the "AppSettings"
/// section in the JSON, and each already has a clear, single place it's consumed.
/// <c>DuplicateCommentOptions</c> is consumed inside Infrastructure
/// (<c>GitHubCommentDuplicateNotifier</c>), but it's still mapped here rather than bound
/// directly in Infrastructure's own DI — Infrastructure can't reference this Api-layer type, so
/// Api derives <c>IOptions&lt;DuplicateCommentOptions&gt;</c> from this instance and registers
/// it before Infrastructure's own registration runs (see <see cref="MiddlewareExtension"/>).
/// </remarks>
public sealed class AppSettings
{
    public const string SectionName = "AppSettings";

    public string[] CorsAllowedOrigins { get; set; } = [];

    public DuplicateDetectionOptions DuplicateDetection { get; set; } = new();

    public DuplicateCommentOptions DuplicateComment { get; set; } = new();
}
