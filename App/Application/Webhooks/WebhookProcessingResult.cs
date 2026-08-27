using IssueSense.Application.DuplicateDetection;

namespace IssueSense.Application.Webhooks;

public sealed record WebhookProcessingResult(
    bool Processed,
    string Reason,
    int DuplicatesFound = 0,
    DuplicateConfidence? HighestConfidence = null);
