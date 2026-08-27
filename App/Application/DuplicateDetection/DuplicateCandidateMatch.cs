using IssueSense.Domain.Enums;

namespace IssueSense.Application.DuplicateDetection;

public sealed record DuplicateCandidateMatch(
    int IssueNumber,
    string Title,
    string Url,
    double SimilarityScore,
    string Repository,
    IssueState State,
    DuplicateConfidence Confidence);
