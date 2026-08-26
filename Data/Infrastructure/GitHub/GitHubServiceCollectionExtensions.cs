using System.Net.Http.Headers;
using IssueSense.Application.GitHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IssueSense.Infrastructure.GitHub;

public static class GitHubServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GitHubOptions>()
            .Bind(configuration.GetSection(GitHubOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.AccessToken),
                "GitHub access token is not configured. Set GitHub:AccessToken via user-secrets, or the " +
                "GitHub__AccessToken environment variable.")
            .ValidateOnStart();

        services.AddTransient<GitHubRateLimitDelegatingHandler>();

        services.AddHttpClient<IGitHubService, GitHubService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
            })
            .AddHttpMessageHandler<GitHubRateLimitDelegatingHandler>()
            .AddStandardResilienceHandler();

        return services;
    }
}
