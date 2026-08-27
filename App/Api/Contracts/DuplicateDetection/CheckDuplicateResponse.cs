namespace IssueSense.Api.Contracts.DuplicateDetection;

/// <summary>The result of checking a new issue against a repository's existing issues.</summary>
/// <param name="IsPotentialDuplicate">True if at least one candidate was classified "Possible" or "HighConfidence".</param>
/// <param name="Confidence">
/// The classification of the strongest match found ("HighConfidence", "Possible", or "Unlikely");
/// "Unlikely" when no candidates were found at all.
/// </param>
/// <param name="Candidates">Existing issues that may be duplicates, most similar first.</param>
/// <param name="Processing">Diagnostic details about how the check was performed.</param>
public sealed record CheckDuplicateResponse(
    bool IsPotentialDuplicate,
    string Confidence,
    IReadOnlyList<DuplicateCandidateResponse> Candidates,
    ProcessingInfoResponse Processing);
