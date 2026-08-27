namespace IssueSense.Api.Contracts.DuplicateDetection;

/// <summary>One existing issue that the checked issue may duplicate.</summary>
/// <param name="IssueNumber">The existing issue's number within its repository.</param>
/// <param name="Title">The existing issue's title.</param>
/// <param name="Url">A link to the existing issue on GitHub.</param>
/// <param name="Similarity">Cosine similarity to the checked issue, from 0.0 to 1.0.</param>
/// <param name="Classification">"HighConfidence", "Possible", or "Unlikely".</param>
public sealed record DuplicateCandidateResponse(
    int IssueNumber,
    string Title,
    string Url,
    double Similarity,
    string Classification);
