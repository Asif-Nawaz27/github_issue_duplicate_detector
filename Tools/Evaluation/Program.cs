using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Embeddings;
using IssueSense.Evaluation;
using IssueSense.Infrastructure.Embeddings;
using Microsoft.Extensions.DependencyInjection;

var options = CliOptions.Parse(args);

if (options.ShowHelp)
{
    CliOptions.PrintUsage();
    return 0;
}

Console.WriteLine($"Loading dataset: {options.DatasetPath}");
var cases = await DatasetLoader.LoadAsync(options.DatasetPath);
Console.WriteLine($"Loaded {cases.Count} cases. Generating embeddings (first run downloads the model — this may take a moment)...");

var services = new ServiceCollection();
services.AddOptions<LocalEmbeddingOptions>();
services.AddHttpClient(LocalEmbeddingService.HttpClientName);
services.AddSingleton<IEmbeddingService, LocalEmbeddingService>();
await using var provider = services.BuildServiceProvider();

var embeddingService = provider.GetRequiredService<IEmbeddingService>();
var evaluator = new SimilarityEvaluator(embeddingService);
var results = await evaluator.ComputeAsync(cases);

var configuredThresholds = new Dictionary<string, double>
{
    [nameof(DuplicateDetectionOptions.MinimumSimilarityThreshold)] = new DuplicateDetectionOptions().MinimumSimilarityThreshold,
    [nameof(DuplicateDetectionOptions.PossibleDuplicateThreshold)] = new DuplicateDetectionOptions().PossibleDuplicateThreshold,
    [nameof(DuplicateDetectionOptions.HighConfidenceThreshold)] = new DuplicateDetectionOptions().HighConfidenceThreshold
};

var report = EvaluationReportWriter.Write(options.DatasetPath, "all-MiniLM-L6-v2", results, options.Thresholds, configuredThresholds);

Console.WriteLine();
Console.WriteLine(report);

if (options.OutputPath is not null)
{
    await File.WriteAllTextAsync(options.OutputPath, report);
    Console.WriteLine($"Report written to: {options.OutputPath}");
}

return 0;

internal sealed class CliOptions
{
    public required string DatasetPath { get; init; }
    public required IReadOnlyList<double> Thresholds { get; init; }
    public string? OutputPath { get; init; }
    public bool ShowHelp { get; init; }

    private static readonly double[] DefaultThresholds = [0.50, 0.60, 0.70, 0.75, 0.80, 0.85, 0.90, 0.95];

    public static CliOptions Parse(string[] args)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var datasetPath = Path.Combine(baseDirectory, "Datasets", "duplicate-detection-eval.json");
        var thresholds = (IReadOnlyList<double>)DefaultThresholds;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dataset" when i + 1 < args.Length:
                    datasetPath = args[++i];
                    break;
                case "--thresholds" when i + 1 < args.Length:
                    thresholds = args[++i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                        .OrderBy(t => t)
                        .ToList();
                    break;
                case "--output" when i + 1 < args.Length:
                    outputPath = args[++i];
                    break;
                case "--help" or "-h":
                    return new CliOptions { DatasetPath = datasetPath, Thresholds = thresholds, ShowHelp = true };
            }
        }

        return new CliOptions { DatasetPath = datasetPath, Thresholds = thresholds, OutputPath = outputPath };
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
            IssueSense duplicate-detection evaluation tool

            Usage:
              dotnet run [-- --dataset <path>] [--thresholds 0.5,0.6,0.7] [--output <path>]

            Options:
              --dataset <path>      Path to a JSON evaluation dataset (default: bundled Datasets/duplicate-detection-eval.json)
              --thresholds <list>   Comma-separated similarity thresholds to sweep (default: 0.50-0.95 in steps of 0.05-0.10)
              --output <path>       Also write the report to this file
              --help                Show this message
            """);
    }
}
