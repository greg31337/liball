namespace icc;

/// <summary>
/// Defines ICC.1 and ICC.2 profile-class signatures.
/// </summary>
public static class IccProfileClassSignatures
{
    /// <summary>The input-device profile signature <c>scnr</c>.</summary>
    public const uint Input = 0x73636e72;

    /// <summary>The display-device profile signature <c>mntr</c>.</summary>
    public const uint Display = 0x6d6e7472;

    /// <summary>The output-device profile signature <c>prtr</c>.</summary>
    public const uint Output = 0x70727472;

    /// <summary>The DeviceLink profile signature <c>link</c>.</summary>
    public const uint DeviceLink = 0x6c696e6b;

    /// <summary>The ColorSpace profile signature <c>spac</c>.</summary>
    public const uint ColorSpace = 0x73706163;

    /// <summary>The abstract profile signature <c>abst</c>.</summary>
    public const uint Abstract = 0x61627374;

    /// <summary>The NamedColor profile signature <c>nmcl</c>.</summary>
    public const uint NamedColor = 0x6e6d636c;

    /// <summary>The ICC.2 ColorEncodingSpace profile signature <c>cenc</c>.</summary>
    public const uint ColorEncodingSpace = 0x63656e63;

    /// <summary>The ICC.2 MultiplexIdentification profile signature <c>mid </c>.</summary>
    public const uint MultiplexIdentification = 0x6d696420;

    /// <summary>The ICC.2 MultiplexLink profile signature <c>mlnk</c>.</summary>
    public const uint MultiplexLink = 0x6d6c6e6b;

    /// <summary>The ICC.2 MultiplexVisualization profile signature <c>mvis</c>.</summary>
    public const uint MultiplexVisualization = 0x6d766973;
}
