using IssueSense.Application.DuplicateDetection;

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
/// Settings that already have their own focused options type and DI wiring — the GitHub
/// integration (<c>GitHubOptions</c>, top-level "GitHub" section) and the database connection
/// string (<c>ConnectionStrings:Postgres</c>) — deliberately stay outside this class rather than
/// being folded in for the sake of "one class for everything"; they're not part of the
/// "AppSettings" section in the JSON, and each already has a clear, single place it's consumed.
/// </remarks>
public sealed class AppSettings
{
    public const string SectionName = "AppSettings";

    public string[] CorsAllowedOrigins { get; set; } = [];

    public DuplicateDetectionOptions DuplicateDetection { get; set; } = new();
}
