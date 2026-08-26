using IssueSense.Application.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IssueSense.Infrastructure.Embeddings;

public static class EmbeddingsServiceCollectionExtensions
{
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
}
