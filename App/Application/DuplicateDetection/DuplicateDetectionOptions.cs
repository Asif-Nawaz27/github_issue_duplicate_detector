namespace IssueSense.Application.DuplicateDetection;

public sealed class DuplicateDetectionOptions
{
    public const string SectionName = "DuplicateDetection";

    /// <summary>How many candidates to return at most.</summary>
    public int TopN { get; set; } = 5;

    /// <summary>
    /// The configurable similarity threshold: candidates below this cosine similarity are not
    /// considered duplicates at all and are excluded from the results entirely.
    /// </summary>
    public double MinimumSimilarityThreshold { get; set; } = 0.50;

    /// <summary>At or above this (and below HighConfidenceThreshold), a candidate is classified "Possible".</summary>
    public double PossibleDuplicateThreshold { get; set; } = 0.75;

    /// <summary>At or above this, a candidate is classified "High confidence".</summary>
    public double HighConfidenceThreshold { get; set; } = 0.90;
}
