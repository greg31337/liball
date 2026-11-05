namespace cbor;

/// <summary>
/// Selects how map keys are ordered during serialization or presentation.
/// </summary>
public enum CborMapKeyOrder
{
    /// <summary>Preserves the order supplied by the caller.</summary>
    Preserve = 0,
    /// <summary>Orders keys lexicographically by their deterministic encodings.</summary>
    Lexicographic = 1,
    /// <summary>Orders keys first by encoded length and then lexicographically.</summary>
    LengthFirst = 2
}
