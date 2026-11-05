namespace sip;

/// <summary>
/// Holds one SDP attribute while distinguishing property and value forms.
/// </summary>
public class SipSdpAttribute
{
    /// <summary>The attribute name.</summary>
    public string? Name;

    /// <summary>The attribute value.</summary>
    public string? Value;

    /// <summary>Indicates whether a colon and value were supplied.</summary>
    public bool HasValue;
}
