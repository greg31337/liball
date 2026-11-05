namespace icc;

/// <summary>
/// Defines registered ICC.2 typed tag-array signatures.
/// </summary>
public static class IccArraySignatures
{
    /// <summary>The generic tag-array signature <c>tags</c>.</summary>
    public const uint Generic = 0x74616773;
    /// <summary>The colorant-information structure-array signature <c>cinf</c>.</summary>
    public const uint ColorantInformation = 0x63696e66;
    /// <summary>The named-colour structure-array signature <c>ncol</c>.</summary>
    public const uint NamedColor = 0x6e636f6c;
    /// <summary>The profile-information structure-array signature <c>pinf</c>.</summary>
    public const uint ProfileInformation = 0x70696e66;
    /// <summary>The multiplex-channel-type array signature <c>mcta</c>.</summary>
    public const uint MultiplexType = 0x6d637461;
}
