namespace cbor;

/// <summary>Selects which native dictionary keys may be mapped to CBOR.</summary>
public enum CborMappingKeyPolicy
{
    /// <summary>Allows every key supported by the object mapper.</summary>
    Any = 0,
    /// <summary>Requires every mapped dictionary key to be a CBOR text string.</summary>
    TextOnly = 1
}
