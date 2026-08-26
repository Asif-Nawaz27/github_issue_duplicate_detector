using IssueSense.Application.Embeddings;
using IssueSense.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace IssueSense.Infrastructure.Embeddings;

/// <summary>
/// Generates embeddings locally via a small sentence-transformer model (all-MiniLM-L6-v2) run
/// through ONNX Runtime, with mean pooling + L2 normalization over the token embeddings — the
/// standard way that model is meant to be used. The model and vocab files are downloaded once
/// (from Hugging Face) into a local cache directory and reused after that; no paid API and no
/// network calls once cached. See <see cref="IEmbeddingService"/> for the swappable contract.
/// </summary>
public sealed class LocalEmbeddingService : IEmbeddingService, IDisposable
{
    public const string HttpClientName = "LocalEmbeddingModelDownload";

    private readonly LocalEmbeddingOptions _options;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private BertTokenizer? _tokenizer;
    private InferenceSession? _session;

    public LocalEmbeddingService(IOptions<LocalEmbeddingOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public async Task<EmbeddingResult> GenerateEmbeddingAsync(string title, string? body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required to generate an embedding.", nameof(title));

        await EnsureInitializedAsync(cancellationToken);

        var text = ComposeText(title, body, _options.MaxInputCharacters);
        var tokenIds = Tokenize(text);

        var pooled = RunInference(tokenIds);

        if (pooled.Length != _options.Dimensions)
        {
            throw new InvalidOperationException(
                $"Configured embedding dimensions ({_options.Dimensions}) do not match the model's actual " +
                $"output size ({pooled.Length}). Check Embeddings:Local:Dimensions against the configured model.");
        }

        return new EmbeddingResult(EmbeddingVector.Create(pooled), _options.ModelName);
    }

    private static string ComposeText(string title, string? body, int maxCharacters)
    {
        var combined = string.IsNullOrWhiteSpace(body) ? title : $"{title}\n\n{body}";
        return combined.Length > maxCharacters ? combined[..maxCharacters] : combined;
    }

    private List<int> Tokenize(string text)
    {
        var ids = _tokenizer!.EncodeToIds(text, addSpecialTokens: true, considerNormalization: true).ToList();

        if (ids.Count > _options.MaxTokens)
        {
            // Keep room for the trailing [SEP] token that marks the end of the sequence.
            ids = [.. ids.Take(_options.MaxTokens - 1), _tokenizer.SeparatorTokenId];
        }

        return ids;
    }

    private float[] RunInference(List<int> tokenIds)
    {
        var sequenceLength = tokenIds.Count;
        var inputIds = new DenseTensor<long>([1, sequenceLength]);
        var attentionMask = new DenseTensor<long>([1, sequenceLength]);
        var tokenTypeIds = new DenseTensor<long>([1, sequenceLength]);

        for (var i = 0; i < sequenceLength; i++)
        {
            inputIds[0, i] = tokenIds[i];
            attentionMask[0, i] = 1;
            tokenTypeIds[0, i] = 0;
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
        };

        if (_session!.InputMetadata.ContainsKey("token_type_ids"))
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds));

        using var outputs = _session.Run(inputs);

        var outputName = _session.OutputMetadata.ContainsKey("last_hidden_state")
            ? "last_hidden_state"
            : _session.OutputMetadata.Keys.First();
        var lastHiddenState = outputs.First(o => o.Name == outputName).AsTensor<float>();

        return MeanPoolAndNormalize(lastHiddenState, sequenceLength);
    }

    private static float[] MeanPoolAndNormalize(Tensor<float> lastHiddenState, int sequenceLength)
    {
        var hiddenSize = lastHiddenState.Dimensions[2];
        var pooled = new float[hiddenSize];

        for (var t = 0; t < sequenceLength; t++)
        {
            for (var h = 0; h < hiddenSize; h++)
                pooled[h] += lastHiddenState[0, t, h];
        }

        for (var h = 0; h < hiddenSize; h++)
            pooled[h] /= sequenceLength;

        var norm = MathF.Sqrt(pooled.Sum(v => v * v));
        if (norm > 0)
        {
            for (var h = 0; h < hiddenSize; h++)
                pooled[h] /= norm;
        }

        return pooled;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_session is not null && _tokenizer is not null)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_session is not null && _tokenizer is not null)
                return;

            Directory.CreateDirectory(_options.ModelDirectory);
            var modelPath = Path.Combine(_options.ModelDirectory, "model.onnx");
            var vocabPath = Path.Combine(_options.ModelDirectory, "vocab.txt");

            await DownloadIfMissingAsync(modelPath, _options.ModelUrl, cancellationToken);
            await DownloadIfMissingAsync(vocabPath, _options.VocabUrl, cancellationToken);

            _tokenizer = BertTokenizer.Create(vocabPath);
            _session = new InferenceSession(modelPath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task DownloadIfMissingAsync(string path, string url, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
            return;

        var tempPath = $"{path}.download";
        await using (var responseStream = await _httpClient.GetStreamAsync(url, cancellationToken))
        await using (var fileStream = File.Create(tempPath))
        {
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _initLock.Dispose();
    }
}
