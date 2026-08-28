using System.Text.Json;
using System.Text.Json.Serialization;

namespace IssueSense.Evaluation;

public static class DatasetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<IReadOnlyList<EvaluationCase>> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Evaluation dataset not found: {path}", path);

        await using var stream = File.OpenRead(path);
        var cases = await JsonSerializer.DeserializeAsync<List<EvaluationCase>>(stream, JsonOptions, cancellationToken);

        return cases is { Count: > 0 }
            ? cases
            : throw new InvalidOperationException($"Evaluation dataset at '{path}' contained no cases.");
    }
}
