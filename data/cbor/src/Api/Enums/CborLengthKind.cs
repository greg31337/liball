namespace cbor;

/// <summary>
/// Identifies whether a CBOR string or container has a definite length.
/// </summary>
public enum CborLengthKind
{
    /// <summary>No length kind has been selected.</summary>
    None = 0,
    /// <summary>The encoded item declares its length.</summary>
    Definite = 1,
    /// <summary>The encoded item ends with a break code.</summary>
    Indefinite = 2
}
