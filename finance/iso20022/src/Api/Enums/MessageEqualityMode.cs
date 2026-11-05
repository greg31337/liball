namespace iso20022;

/// <summary>Identifies the representation used for deterministic message equality and checksums.</summary>
public enum MessageEqualityMode
{
    /// <summary>The equality mode is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The exact supplied bytes are compared or hashed.</summary>
    Byte = 1,
    /// <summary>Canonical XML bytes are compared or hashed.</summary>
    CanonicalXml = 2,
    /// <summary>The normalized semantic message model is compared or hashed.</summary>
    SemanticModel = 3
}
