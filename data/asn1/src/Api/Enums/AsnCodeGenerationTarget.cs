namespace asn1;

/// <summary>
/// Identifies the artifacts produced by C# generation.
/// </summary>
public enum AsnCodeGenerationTarget
{
    /// <summary>
    /// Represents model only.
    /// </summary>
    ModelOnly = 0,

    /// <summary>
    /// Represents descriptor only.
    /// </summary>
    DescriptorOnly = 1,

    /// <summary>
    /// Represents encode only.
    /// </summary>
    EncodeOnly = 2,

    /// <summary>
    /// Represents decode only.
    /// </summary>
    DecodeOnly = 3,

    /// <summary>
    /// Represents combined.
    /// </summary>
    Combined = 4
}
