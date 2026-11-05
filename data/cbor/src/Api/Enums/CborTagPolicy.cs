namespace cbor;

/// <summary>
/// Selects how semantic tags are handled while processing CBOR.
/// </summary>
public enum CborTagPolicy
{
    /// <summary>Preserves known and unknown tags without semantic validation.</summary>
    Preserve = 0,
    /// <summary>Validates registered tags and preserves unknown tags.</summary>
    ValidateKnown = 1,
    /// <summary>Validates registered tags and rejects unknown tags.</summary>
    RejectUnknown = 2,
    /// <summary>Rejects every semantic tag.</summary>
    Forbid = 3
}
