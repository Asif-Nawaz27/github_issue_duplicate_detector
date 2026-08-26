namespace IssueSense.Infrastructure.Embeddings;

public sealed class LocalEmbeddingOptions
{
    public const string SectionName = "Embeddings:Local";

    /// <summary>Directory the ONNX model and vocab files are downloaded to and cached in.</summary>
    public string ModelDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IssueSense", "embedding-models");

    public string ModelUrl { get; set; } = "https://huggingface.co/Xenova/all-MiniLM-L6-v2/resolve/main/onnx/model_quantized.onnx";

    public string VocabUrl { get; set; } = "https://huggingface.co/Xenova/all-MiniLM-L6-v2/resolve/main/vocab.txt";

    /// <summary>Stored alongside every embedding so a later model swap never gets compared against stale vectors.</summary>
    public string ModelName { get; set; } = "all-MiniLM-L6-v2";

    /// <summary>
    /// Expected output size of the configured model. Validated against what the model actually
    /// produces at inference time, so a mismatched model/config pairing fails loudly instead of
    /// silently writing vectors of the wrong size.
    /// </summary>
    public int Dimensions { get; set; } = 384;

    /// <summary>Sequence length the model was trained with; longer input is truncated to this many tokens.</summary>
    public int MaxTokens { get; set; } = 256;

    /// <summary>Cheap pre-tokenization guard against pathologically large input.</summary>
    public int MaxInputCharacters { get; set; } = 8000;
}
