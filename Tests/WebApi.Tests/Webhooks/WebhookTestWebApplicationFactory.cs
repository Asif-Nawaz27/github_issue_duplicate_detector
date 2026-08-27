using IssueSense.Application.DuplicateDetection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IssueSense.WebApi.Tests.Webhooks;

/// <summary>
/// Hosts the real app (real controllers, DI wiring, middleware) but swaps the duplicate
/// detection service for a fake, so these tests exercise the actual webhook endpoint's
/// signature verification/parsing/routing without needing Postgres or the ONNX model.
/// </summary>
public sealed class WebhookTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string WebhookSecret = "test-webhook-secret";

    public FakeDuplicateDetectionService DuplicateDetectionService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Syntactically valid but never actually connected to: nothing in these tests
                // touches the database, since IDuplicateDetectionService is replaced below.
                ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test",
                ["GitHub:AccessToken"] = "test-access-token",
                ["GitHub:WebhookSecret"] = WebhookSecret
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDuplicateDetectionService>();
            services.AddSingleton<IDuplicateDetectionService>(DuplicateDetectionService);
        });
    }
}
