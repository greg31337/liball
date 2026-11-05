namespace sdp;

/// <summary>
/// Represents a generic SDP property or value attribute.
/// </summary>
public class SdpAttribute
{
    /// <summary>Identifies whether the attribute has a colon-delimited value.</summary>
    public SdpAttributeKind Kind = SdpAttributeKind.Unknown;

    /// <summary>Contains the attribute name with its original spelling.</summary>
    public string Name = string.Empty;

    /// <summary>Contains the attribute value, including an explicitly empty value.</summary>
    public string Value = string.Empty;

    /// <summary>Contains the original attribute text following <c>a=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the attribute's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
