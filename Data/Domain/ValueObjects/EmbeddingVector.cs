namespace IssueSense.Domain.ValueObjects;

public sealed class EmbeddingVector : IEquatable<EmbeddingVector>
{
    private readonly float[] _values;

    public IReadOnlyList<float> Values => _values;

    public int Dimension => _values.Length;

    private EmbeddingVector(float[] values) => _values = values;

    public static EmbeddingVector Create(IReadOnlyCollection<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
            throw new ArgumentException("Embedding vector cannot be empty.", nameof(values));

        return new EmbeddingVector(values.ToArray());
    }

    public bool Equals(EmbeddingVector? other) =>
        other is not null && (ReferenceEquals(this, other) || _values.AsSpan().SequenceEqual(other._values));

    public override bool Equals(object? obj) => Equals(obj as EmbeddingVector);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _values)
            hash.Add(value);

        return hash.ToHashCode();
    }

    public static bool operator ==(EmbeddingVector? left, EmbeddingVector? right) => Equals(left, right);

    public static bool operator !=(EmbeddingVector? left, EmbeddingVector? right) => !Equals(left, right);

    public override string ToString() => $"EmbeddingVector[{Dimension}]";
}
