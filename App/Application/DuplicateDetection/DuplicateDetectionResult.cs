namespace IssueSense.Application.DuplicateDetection;

public sealed record DuplicateDetectionResult(
    IReadOnlyList<DuplicateCandidateMatch> Candidates,
    string EmbeddingModelUsed,
    double SimilarityThresholdUsed);
