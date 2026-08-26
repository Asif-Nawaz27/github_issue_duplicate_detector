using IssueSense.Application.Embeddings;
using IssueSense.Application.Import;
using Microsoft.Extensions.DependencyInjection;

namespace IssueSense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IIssueImportService, IssueImportService>();
        services.AddScoped<IEmbeddingGenerationService, EmbeddingGenerationService>();

        return services;
    }
}
