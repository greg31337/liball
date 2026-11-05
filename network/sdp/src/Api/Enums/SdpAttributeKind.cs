namespace sdp;

/// <summary>
/// Identifies the syntactic form of an SDP attribute.
/// </summary>
public enum SdpAttributeKind
{
    /// <summary>The attribute form is unknown.</summary>
    Unknown = 0,

    /// <summary>The attribute is a property with no colon or value.</summary>
    Property = 1,

    /// <summary>The attribute has a colon-delimited value, which may be empty.</summary>
    Value = 2
}
