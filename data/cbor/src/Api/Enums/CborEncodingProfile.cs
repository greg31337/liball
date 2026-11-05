namespace cbor;

/// <summary>
/// Selects the serialization requirements applied by a CBOR encoder.
/// </summary>
public enum CborEncodingProfile
{
    /// <summary>Produces well-formed CBOR without additional representation requirements.</summary>
    Basic = 0,
    /// <summary>Uses preferred serialization choices where possible.</summary>
    Preferred = 1,
    /// <summary>Uses RFC 8949 core deterministic encoding.</summary>
    CoreDeterministic = 2,
    /// <summary>Uses RFC 8949 length-first core deterministic encoding.</summary>
    LengthFirstCoreDeterministic = 3
}
