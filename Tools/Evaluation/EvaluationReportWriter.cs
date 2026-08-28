using System.Globalization;
using System.Text;

namespace IssueSense.Evaluation;

public static class EvaluationReportWriter
{
    public static string Write(
        string datasetPath,
        string embeddingModel,
        IReadOnlyList<SimilarityResult> results,
        IReadOnlyList<double> thresholds,
        IReadOnlyDictionary<string, double>? configuredThresholds = null)
    {
        var sb = new StringBuilder();

        WriteHeader(sb, datasetPath, embeddingModel, results);
        WriteSimilarityDistribution(sb, results);

        var metricsByThreshold = thresholds
            .Select(t => MetricsCalculator.Calculate(results, t))
            .ToList();

        WriteThresholdSweep(sb, metricsByThreshold);

        var best = metricsByThreshold.OrderByDescending(m => m.F1Score).ThenBy(m => m.Threshold).First();
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Best F1 in this sweep: threshold={best.Threshold:F2} (F1={best.F1Score:F3}, Precision={best.Precision:F3}, Recall={best.Recall:F3})");

        if (configuredThresholds is { Count: > 0 })
            WriteConfiguredThresholds(sb, results, configuredThresholds);

        WriteMisclassifications(sb, results, best.Threshold);

        return sb.ToString();
    }

    private static void WriteHeader(StringBuilder sb, string datasetPath, string embeddingModel, IReadOnlyList<SimilarityResult> results)
    {
        var byRelationship = results.GroupBy(r => r.Case.ExpectedRelationship).ToDictionary(g => g.Key, g => g.Count());

        sb.AppendLine("=== Duplicate Detection Evaluation Report ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Dataset: {datasetPath}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Embedding model: {embeddingModel}");
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"Cases: {results.Count} (Duplicate: {byRelationship.GetValueOrDefault(IssueRelationship.Duplicate)}, " +
            $"Related: {byRelationship.GetValueOrDefault(IssueRelationship.Related)}, " +
            $"Unrelated: {byRelationship.GetValueOrDefault(IssueRelationship.Unrelated)})");
        sb.AppendLine();
    }

    private static void WriteSimilarityDistribution(StringBuilder sb, IReadOnlyList<SimilarityResult> results)
    {
        sb.AppendLine("--- Similarity score distribution by expected relationship ---");

        foreach (var relationship in Enum.GetValues<IssueRelationship>())
        {
            var scores = results.Where(r => r.Case.ExpectedRelationship == relationship).Select(r => r.Similarity).ToList();
            if (scores.Count == 0)
                continue;

            sb.AppendLine(CultureInfo.InvariantCulture,
                $"{relationship,-10}: min={scores.Min():F3}  mean={scores.Average():F3}  max={scores.Max():F3}  (n={scores.Count})");
        }

        sb.AppendLine();
    }

    private static void WriteThresholdSweep(StringBuilder sb, IReadOnlyList<ThresholdMetrics> metrics)
    {
        sb.AppendLine("--- Threshold sweep (Duplicate = positive class) ---");
        sb.AppendLine("Threshold | TP | FP | TN | FN | Precision | Recall | F1    | FPR   | FNR");

        foreach (var m in metrics)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"{m.Threshold,9:F2} | {m.TruePositives,2} | {m.FalsePositives,2} | {m.TrueNegatives,2} | {m.FalseNegatives,2} | " +
                $"{m.Precision,9:F3} | {m.Recall,6:F3} | {m.F1Score,5:F3} | {m.FalsePositiveRate,5:F3} | {m.FalseNegativeRate,5:F3}");
        }
    }

    private static void WriteConfiguredThresholds(
        StringBuilder sb, IReadOnlyList<SimilarityResult> results, IReadOnlyDictionary<string, double> configuredThresholds)
    {
        sb.AppendLine();
        sb.AppendLine("--- Currently configured DuplicateDetectionOptions thresholds ---");

        foreach (var (name, value) in configuredThresholds)
        {
            var m = MetricsCalculator.Calculate(results, value);
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"{name,-26} = {value:F2}  ->  Precision={m.Precision:F3} Recall={m.Recall:F3} F1={m.F1Score:F3} FPR={m.FalsePositiveRate:F3} FNR={m.FalseNegativeRate:F3}");
        }
    }

    private static void WriteMisclassifications(StringBuilder sb, IReadOnlyList<SimilarityResult> results, double threshold)
    {
        var misclassified = results
            .Where(r =>
                (r.Case.ExpectedRelationship == IssueRelationship.Duplicate) != (r.Similarity >= threshold))
            .OrderBy(r => r.Case.Id)
            .ToList();

        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"--- Misclassifications at threshold={threshold:F2} ({misclassified.Count} of {results.Count}) ---");

        if (misclassified.Count == 0)
        {
            sb.AppendLine("(none)");
            return;
        }

        foreach (var r in misclassified)
        {
            var predicted = r.Similarity >= threshold ? "flagged as duplicate" : "not flagged";
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"[{r.Case.Id}] expected={r.Case.ExpectedRelationship}, similarity={r.Similarity:F3}, predicted={predicted}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    new:      \"{r.Case.NewIssue.Title}\"");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    existing: \"{r.Case.ExistingIssue.Title}\"");
        }
    }
}
