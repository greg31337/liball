namespace cbor;

/// <summary>
/// Holds one key and value in an ordered CBOR map.
/// </summary>
public class CborMapEntry
{
    /// <summary>The map key, which may be any CBOR value.</summary>
    public CborValue? Key;
    /// <summary>The map value.</summary>
    public CborValue? Value;
}
