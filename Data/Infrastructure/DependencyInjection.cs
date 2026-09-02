using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Embeddings;
using IssueSense.Application.GitHub;
using IssueSense.Application.Persistence;
using IssueSense.Application.Webhooks;
using IssueSense.Infrastructure.Embeddings;
using IssueSense.Infrastructure.GitHub;
using IssueSense.Infrastructure.GitHub.Webhooks;
using IssueSense.Infrastructure.Persistence;
using IssueSense.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net.Http.Headers;

namespace IssueSense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IRepositoryRepository, RepositoryRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IIssueEmbeddingRepository, IssueEmbeddingRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }



    public static IServiceCollection AddEmbeddings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalEmbeddingOptions>()
            .Bind(configuration.GetSection(LocalEmbeddingOptions.SectionName));

        services.AddHttpClient(LocalEmbeddingService.HttpClientName);

        // Singleton: the ONNX InferenceSession is expensive to create and safe for concurrent
        // inference calls, so one instance is loaded once and reused for the app's lifetime.
        services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();

        return services;
    }

    public static IServiceCollection AddGitHubClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GitHubOptions>()
            .Bind(configuration.GetSection(GitHubOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.AccessToken),
                "GitHub access token is not configured. Set GitHub:AccessToken via user-secrets, or the " +
                "GitHub__AccessToken environment variable.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.WebhookSecret),
                "GitHub webhook secret is not configured. Set GitHub:WebhookSecret via user-secrets, or the " +
                "GitHub__WebhookSecret environment variable.")
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

        services.AddSingleton<IGitHubWebhookSignatureVerifier, GitHubWebhookSignatureVerifier>();
        services.AddSingleton<IGitHubWebhookParser, GitHubWebhookPayloadParser>();
        services.AddScoped<IDuplicateNotifier, GitHubCommentDuplicateNotifier>();

        // DuplicateCommentOptions is registered by the caller (App/Api's MiddlewareExtension),
        // derived from AppSettings, rather than bound here from raw configuration.

        return services;
    }
}
