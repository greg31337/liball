namespace sdp;

/// <summary>
/// Represents an SDP bandwidth field.
/// </summary>
public class SdpBandwidth
{
    /// <summary>Contains the registered or extension bandwidth-type token.</summary>
    public string Type = string.Empty;

    /// <summary>Contains the parsed nonnegative bandwidth value.</summary>
    public long Value = 0;

    /// <summary>Contains the original bandwidth value text for extension-safe preservation.</summary>
    public string RawValue = string.Empty;

    /// <summary>Contains the original text following <c>b=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the bandwidth field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
