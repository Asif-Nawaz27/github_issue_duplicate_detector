using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Embeddings;
using IssueSense.Application.Import;
using IssueSense.Application.Owners;
using IssueSense.Application.Repositories;
using IssueSense.Application.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IssueSense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIssueImportService, IssueImportService>();
        services.AddScoped<IEmbeddingGenerationService, EmbeddingGenerationService>();
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IGitHubIssueWebhookHandler, GitHubIssueWebhookHandler>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IRepositoryLookupService, RepositoryLookupService>();

        services.AddOptions<DuplicateDetectionOptions>()
            .Bind(configuration.GetSection(DuplicateDetectionOptions.SectionName));

        return services;
    }
}
