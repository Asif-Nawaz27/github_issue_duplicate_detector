namespace IssueSense.Api.Contracts.DuplicateDetection;

/// <summary>Diagnostic details about how the duplicate check was performed.</summary>
/// <param name="EmbeddingModel">The embedding model used to generate the vector for this check.</param>
/// <param name="SimilarityThreshold">The minimum similarity a candidate needed to be included at all.</param>
/// <param name="ProcessingTimeMs">Total time to process the request, in milliseconds.</param>
public sealed record ProcessingInfoResponse(
    string EmbeddingModel,
    double SimilarityThreshold,
    long ProcessingTimeMs);
