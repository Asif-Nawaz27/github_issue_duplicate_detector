namespace IssueSense.Evaluation;

/// <summary>
/// Confusion-matrix metrics for one similarity threshold. "Duplicate" is the positive class;
/// "Related" and "Unrelated" are both negative — a Related pair that scores above the threshold
/// is a false positive, since it's similar but genuinely not a duplicate. That's deliberate: it's
/// exactly the distinction duplicate detection has to get right to be useful.
/// </summary>
public sealed record ThresholdMetrics(
    double Threshold,
    int TruePositives,
    int FalsePositives,
    int TrueNegatives,
    int FalseNegatives,
    double Precision,
    double Recall,
    double F1Score,
    double FalsePositiveRate,
    double FalseNegativeRate);

public static class MetricsCalculator
{
    public static ThresholdMetrics Calculate(IReadOnlyList<SimilarityResult> results, double threshold)
    {
        int tp = 0, fp = 0, tn = 0, fn = 0;

        foreach (var result in results)
        {
            var actualPositive = result.Case.ExpectedRelationship == IssueRelationship.Duplicate;
            var predictedPositive = result.Similarity >= threshold;

            switch (actualPositive, predictedPositive)
            {
                case (true, true): tp++; break;
                case (false, true): fp++; break;
                case (false, false): tn++; break;
                case (true, false): fn++; break;
            }
        }

        var precision = tp + fp == 0 ? 0.0 : (double)tp / (tp + fp);
        var recall = tp + fn == 0 ? 0.0 : (double)tp / (tp + fn);
        var f1 = precision + recall == 0 ? 0.0 : 2 * precision * recall / (precision + recall);
        var fpr = fp + tn == 0 ? 0.0 : (double)fp / (fp + tn);
        var fnr = fn + tp == 0 ? 0.0 : (double)fn / (fn + tp);

        return new ThresholdMetrics(threshold, tp, fp, tn, fn, precision, recall, f1, fpr, fnr);
    }
}
