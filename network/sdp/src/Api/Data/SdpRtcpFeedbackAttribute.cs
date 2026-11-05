namespace sdp;

/// <summary>Represents a typed RTCP feedback attribute.</summary>
public class SdpRtcpFeedbackAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains a numeric payload token or <c>*</c>.</summary>
    public string PayloadType = string.Empty;
    /// <summary>Contains the registered or extension feedback type.</summary>
    public string Type = string.Empty;
    /// <summary>Indicates whether a feedback parameter was present.</summary>
    public bool HasParameter = false;
    /// <summary>Contains the feedback subtype or parameter text.</summary>
    public string Parameter = string.Empty;
}
