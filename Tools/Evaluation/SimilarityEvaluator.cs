using IssueSense.Application.Embeddings;

namespace IssueSense.Evaluation;

public sealed record SimilarityResult(EvaluationCase Case, double Similarity);

/// <summary>Runs each case's two issues through the real embedding service and scores their cosine similarity.</summary>
public sealed class SimilarityEvaluator(IEmbeddingService embeddingService)
{
    public async Task<IReadOnlyList<SimilarityResult>> ComputeAsync(
        IEnumerable<EvaluationCase> cases, CancellationToken cancellationToken = default)
    {
        var results = new List<SimilarityResult>();

        foreach (var evaluationCase in cases)
        {
            var newEmbedding = await embeddingService.GenerateEmbeddingAsync(
                evaluationCase.NewIssue.Title, evaluationCase.NewIssue.Body, cancellationToken);
            var existingEmbedding = await embeddingService.GenerateEmbeddingAsync(
                evaluationCase.ExistingIssue.Title, evaluationCase.ExistingIssue.Body, cancellationToken);

            var similarity = CosineSimilarity(newEmbedding.Vector.Values, existingEmbedding.Vector.Values);
            results.Add(new SimilarityResult(evaluationCase, similarity));
        }

        return results;
    }

    private static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
