namespace IssueSense.Domain.ValueObjects;

/// <summary>Cosine similarity between two issue embeddings; mathematically bounded to [-1.0, 1.0].</summary>
public readonly record struct SimilarityScore
{
    public double Value { get; }

    private SimilarityScore(double value) => Value = value;

    public static SimilarityScore Create(double value)
    {
        if (value is < -1.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Similarity score must be between -1.0 and 1.0.");

        return new SimilarityScore(value);
    }

    public override string ToString() => Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
}
